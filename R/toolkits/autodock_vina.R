autodock_vina <- function(prot_pdb, ligand_pdb, complex_pdb, score_txt) {
  
  # 加载必要的R包
  if (!requireNamespace("tools", quietly = TRUE)) {
    stop("Required package 'tools' is not installed.")
  }
  
  # 定义MGLTools和AutoDock Vina的安装路径（根据用户设置）
  mgltools_dir <- "/opt/mgltools"
  autodock_vina_dir <- "/opt/autodock"
  
  # 构建MGLTools实用工具脚本的完整路径
  prepare_receptor <- file.path(mgltools_dir, "MGLToolsPckgs/AutoDockTools/Utilities24/prepare_receptor4.py")
  prepare_ligand <- file.path(mgltools_dir, "MGLToolsPckgs/AutoDockTools/Utilities24/prepare_ligand4.py")
  
  # 定义Vina可执行文件的路径
  vina_exe <- file.path(autodock_vina_dir, "vina")
  
  # 检查必要工具是否存在
  if (!file.exists(prepare_receptor)) {
    stop("MGLTools prepare_receptor4.py script not found at: ", prepare_receptor)
  }
  if (!file.exists(prepare_ligand)) {
    stop("MGLTools prepare_ligand4.py script not found at: ", prepare_ligand)
  }
  if (!file.exists(vina_exe)) {
    stop("AutoDock Vina executable not found at: ", vina_exe)
  }
  
  # 为临时文件创建唯一的工作目录
  temp_dir <- tempdir()
  orig_wd <- getwd()
  on.exit(setwd(orig_wd), add = TRUE) # 确保无论函数如何退出，工作目录都会恢复
  
  # 切换到临时目录进行处理
  setwd(temp_dir)
  
  # 1. 准备受体（蛋白质）
  message("Preparing receptor (protein)...")
  prot_pdbqt <- file.path(temp_dir, "protein.pdbqt")
  receptor_cmd <- paste(
    "python2", prepare_receptor, 
    "-r", prot_pdb, 
    "-o", prot_pdbqt,
    "-A hydrogens" # 添加氢原子
  )
  system(receptor_cmd)
  
  # 检查受体预处理是否成功
  if (!file.exists(prot_pdbqt)) {
    stop("Failed to prepare receptor PDBQT file.")
  }
  
  # 2. 准备配体（小分子）
  message("Preparing ligand...")
  ligand_pdbqt <- file.path(temp_dir, "ligand.pdbqt")
  ligand_cmd <- paste(
    "python2", prepare_ligand, 
    "-l", ligand_pdb, 
    "-o", ligand_pdbqt,
    "-A hydrogens" # 添加氢原子
  )
  system(ligand_cmd)
  
  # 检查配体预处理是否成功
  if (!file.exists(ligand_pdbqt)) {
    stop("Failed to prepare ligand PDBQT file.")
  }
  
  # 3. 生成Vina配置文件
  message("Configuring docking box...")
  
  # 估算对接盒子中心（基于受体质心）
  # 注意：这是一个简化的方法。更精确的做法是使用已知结合位点或配体位置[1](@ref)
  con <- file(prot_pdb, "r")
  coords <- list()
  while (length(line <- readLines(con, n = 1)) > 0) {
    if (startsWith(line, "ATOM")) {
      x <- as.numeric(substr(line, 31, 38))
      y <- as.numeric(substr(line, 39, 46))
      z <- as.numeric(substr(line, 47, 54))
      coords$x <- c(coords$x, x)
      coords$y <- c(coords$y, y)
      coords$z <- c(coords$z, z)
    }
  }
  close(con)
  
  center_x <- mean(coords$x, na.rm = TRUE)
  center_y <- mean(coords$y, na.rm = TRUE)
  center_z <- mean(coords$z, na.rm = TRUE)
  
  # 创建配置文件
  config_file <- file.path(temp_dir, "config.txt")
  writeLines(
    c(
      paste("receptor =", prot_pdbqt),
      paste("ligand =", ligand_pdbqt),
      paste("center_x =", round(center_x, 4)),
      paste("center_y =", round(center_y, 4)),
      paste("center_z =", round(center_z, 4)),
      "size_x = 25.0",
      "size_y = 25.0",
      "size_z = 25.0",
      "num_modes = 10",       # 生成10个构象
      "energy_range = 4",    # 能量范围
      "exhaustiveness = 8"   # 搜索强度[7](@ref)
    ),
    config_file
  )
  
  # 4. 运行AutoDock Vina对接
  message("Running AutoDock Vina...")
  output_pdbqt <- file.path(temp_dir, "output.pdbqt")
  log_file <- file.path(temp_dir, "vina_log.txt")
  
  vina_cmd <- paste(
    vina_exe,
    "--config", config_file,
    "--out", output_pdbqt,
    "--log", log_file
  )
  system(vina_cmd)
  
  # 检查对接是否成功完成
  if (!file.exists(output_pdbqt)) {
    stop("AutoDock Vina docking failed. Check the log for errors.")
  }
  
  # 5. 处理对接结果
  message("Processing results...")
  
  # 读取日志文件以提取得分
  log_content <- readLines(log_file)
  score_lines <- grep("^.*mode.*affinity.*$", log_content, value = TRUE, ignore.case = TRUE)
  
  # 将得分信息写入指定文件
  writeLines(score_lines, score_txt)
  message("Scores written to: ", score_txt)
  
  # 提取Top1构象（能量最低的构象）
  # Vina的输出PDBQT文件中，不同构象由MODEL和ENDMDL记录分隔[1](@ref)
  pdbqt_content <- readLines(output_pdbqt)
  model_starts <- grep("^MODEL", pdbqt_content)
  model_ends <- grep("^ENDMDL", pdbqt_content)
  
  if (length(model_starts) > 0) {
    # 提取第一个模型（Top1）
    top1_start <- model_starts[1]
    top1_end <- model_ends[1]
    top1_lines <- pdbqt_content[top1_start:top1_end]
    
    # 将Top1构象写入复合体输出文件
    writeLines(top1_lines, complex_pdb)
    message("Top1 complex structure written to: ", complex_pdb)
  } else {
    stop("No models found in the docking output.")
  }
  
  message("Molecular docking completed successfully.")
  
  # 清理：可以选择删除临时文件，但暂时保留以供调试
  # unlink(temp_dir, recursive = TRUE)
}