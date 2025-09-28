#!/bin/bash

# =============================================
# 分子对接自动化脚本
# 使用MGLTools和AutoDock Vina
# =============================================
readonly prot_pdb="{$prot_pdb}"
readonly ligand_pdb="{$ligand_pdb}"
readonly temp_dir="{$temp_dir}"

# 检查必需变量是否设置
if [ -z "$prot_pdb" ] || [ -z "$ligand_pdb" ]; then
    echo "错误: 必须设置prot_pdb和ligand_pdb变量"
    exit 1
fi

# 设置默认变量（如果未提供）
: ${center:="null"}
: ${size:="25.0 25.0 25.0"}
: ${num_modes:=10}
: ${energy_range:=4}
: ${exhaustiveness:=8}
: ${cpu:=1}
: ${seed:=1}
: ${mgltools_dir:="/opt/mgltools"}
: ${autodock_vina_dir:="/opt/autodock_vina"}

# 创建输出目录
mkdir -p "$temp_dir"

# 检查MGLTools和Vina路径
if [ ! -d "$mgltools_dir" ]; then
    echo "错误: MGLTools目录不存在 - $mgltools_dir"
    exit 1
fi
if [ ! -d "$autodock_vina_dir" ]; then
    echo "错误: AutoDock Vina目录不存在 - $autodock_vina_dir"
    exit 1
fi

# 定义工具路径
mgltools_utils="$mgltools_dir/MGLToolsPckgs/AutoDockTools/Utilities24"
prepare_receptor="$mgltools_utils/prepare_receptor4.py"
prepare_ligand="$mgltools_utils/prepare_ligand4.py"
pythonsh="$mgltools_dir/bin/pythonsh"
vina="$autodock_vina_dir/bin/vina"

# 检查必需工具是否存在
for tool in "$prepare_receptor" "$prepare_ligand" "$pythonsh" "$vina"; do
    if [ ! -f "$tool" ]; then
        echo "错误: 未找到工具 - $tool"
        exit 1
    fi
done

# 检查输入文件是否存在
if [ ! -f "$prot_pdb" ]; then
    echo "错误: 蛋白质PDB文件不存在 - $prot_pdb"
    exit 1
fi
if [ ! -f "$ligand_pdb" ]; then
    echo "错误: 配体PDB文件不存在 - $ligand_pdb"
    exit 1
fi

# =============================================
# 1. 准备受体（蛋白质）文件 - 转换为PDBQT格式
# =============================================
echo "步骤1: 准备受体文件..."
receptor_pdbqt="${temp_dir}/receptor.pdbqt"
if ! $pythonsh "$prepare_receptor" -r "$prot_pdb" -o "$receptor_pdbqt"; then
    echo "错误: 受体准备失败"
    exit 1
fi
if [ ! -f "$receptor_pdbqt" ]; then
    echo "错误: 受体PDBQT文件未生成"
    exit 1
fi

# =============================================
# 2. 准备配体（小分子）文件 - 转换为PDBQT格式
# =============================================
echo "步骤2: 准备配体文件..."
ligand_pdbqt="${temp_dir}/ligand.pdbqt"
if ! $pythonsh "$prepare_ligand" -l "$ligand_pdb" -o "$ligand_pdbqt"; then
    echo "错误: 配体准备失败"
    exit 1
fi
if [ ! -f "$ligand_pdbqt" ]; then
    echo "错误: 配体PDBQT文件未生成"
    exit 1
fi

# =============================================
# 3. 计算对接盒子中心（如果未提供）
# =============================================
if [ "$center" = "null" ]; then
    echo "计算对接盒子中心（从配体文件）..."
    # 使用awk从配体PDB文件计算几何中心
    center_calc=$(awk '
    /^ATOM|^HETATM/ {
        if ($6 ~ /^[-]?[0-9]/) {  # 检查第6列是否为数字（x坐标）
            x_sum += $6
            y_sum += $7
            z_sum += $8
            count++
        }
    }
    END {
        if (count > 0) {
            printf "%.3f %.3f %.3f", x_sum/count, y_sum/count, z_sum/count
        } else {
            print "ERROR"
        }
    }' "$ligand_pdb")
    
    if [ "$center_calc" = "ERROR" ]; then
        echo "错误: 无法从配体文件计算中心坐标"
        exit 1
    fi
    center_x=$(echo "$center_calc" | awk '{print $1}')
    center_y=$(echo "$center_calc" | awk '{print $2}')
    center_z=$(echo "$center_calc" | awk '{print $3}')
else
    # 使用用户提供的中心坐标（假设格式为"x y z"）
    center_x=$(echo "$center" | awk '{print $1}')
    center_y=$(echo "$center" | awk '{print $2}')
    center_z=$(echo "$center" | awk '{print $3}')
fi

# 解析盒子尺寸（假设为空格分隔的字符串）
size_x=$(echo "$size" | awk '{print $1}')
size_y=$(echo "$size" | awk '{print $2}')
size_z=$(echo "$size" | awk '{print $3}')

echo "对接参数: 中心($center_x, $center_y, $center_z) 尺寸($size_x, $size_y, $size_z)"

# =============================================
# 4. 运行AutoDock Vina对接
# =============================================
echo "步骤3: 运行分子对接..."
output_pdbqt="${temp_dir}/output.pdbqt"
log_file="${temp_dir}/vina.log"

# Input:
# --receptor arg        rigid part of the receptor (PDBQT)
# --flex arg            flexible side chains, if any (PDBQT)
# --ligand arg          ligand (PDBQT)

# Search space (required):
# --center_x arg        X coordinate of the center
# --center_y arg        Y coordinate of the center
# --center_z arg        Z coordinate of the center
# --size_x arg          size in the X dimension (Angstroms)
# --size_y arg          size in the Y dimension (Angstroms)
# --size_z arg          size in the Z dimension (Angstroms)

# Output (optional):
# --out arg             output models (PDBQT), the default is chosen based on
#                         the ligand file name
# --log arg             optionally, write log file

# Misc (optional):
# --cpu arg                 the number of CPUs to use (the default is to try to
#                             detect the number of CPUs or, failing that, use 1)
# --seed arg                explicit random seed
# --exhaustiveness arg (=8) exhaustiveness of the global search (roughly
#                             proportional to time): 1+
# --num_modes arg (=9)      maximum number of binding modes to generate
# --energy_range arg (=3)   maximum energy difference between the best binding
#                             mode and the worst one displayed (kcal/mol)

# Configuration file (optional):
# --config arg          the above options can be put here

# Information (optional):
# --help                display usage summary
# --help_advanced       display usage summary with advanced options
# --version             display program version

# 执行对接命令
if ! "$vina" \
    --receptor "$receptor_pdbqt" \
    --ligand "$ligand_pdbqt" \
    --center_x "$center_x" \
    --center_y "$center_y" \
    --center_z "$center_z" \
    --size_x "$size_x" \
    --size_y "$size_y" \
    --size_z "$size_z" \
    --num_modes "$num_modes" \
    --energy_range "$energy_range" \
    --exhaustiveness "$exhaustiveness" \
    --cpu "$cpu" \
    --seed "$seed" \
    --out "$output_pdbqt" \
    --log "$log_file"; then
    echo "错误: 分子对接失败"
    exit 1
fi

# =============================================
# 5. 检查结果
# =============================================
if [ -f "$output_pdbqt" ]; then
    echo "对接成功完成!"
    echo "结果文件: $output_pdbqt"
    echo "日志文件: $log_file"
    
    # 显示对接结果摘要
    if [ -f "$log_file" ]; then
        echo "=== 对接结果摘要 ==="
        grep -A 5 "mode" "$log_file" | head -10
    fi
else
    echo "错误: 未生成输出文件"
    exit 1
fi

echo "分子对接流程已完成!"