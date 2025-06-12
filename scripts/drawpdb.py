import os
import sys
from Bio.PDB import PDBParser, MMCIFParser
import py3Dmol
from PIL import Image
from matplotlib.backends.backend_pdf import PdfPages
import matplotlib.pyplot as plt

# pip install biopython py3Dmol matplotlib pillow

def visualize_pdb(pdb_file, output_png, output_pdf, width=800, height=600):
    """
    可视化PDB文件并生成PNG和PDF格式图像
    :param pdb_file: 输入PDB文件路径
    :param output_png: 输出PNG文件路径
    :param output_pdf: 输出PDF文件路径
    :param width: 图像宽度(像素)
    :param height: 图像高度(像素)
    """
    # 1. 解析PDB文件[9,12](@ref)
    try:
        if pdb_file.endswith('.cif'):
            parser = MMCIFParser(QUIET=True)
        else:
            parser = PDBParser(QUIET=True)
        structure = parser.get_structure("protein", pdb_file)
    except Exception as e:
        print(f"PDB解析错误: {str(e)}")
        sys.exit(1)

    # 2. 创建3D可视化视图[12,13](@ref)
    viewer = py3Dmol.view(width=width, height=height)
    viewer.addModel(open(pdb_file, 'r').read(), 'pdb')
    
    # 设置蛋白质可视化样式
    viewer.setStyle({'cartoon': {'color': 'spectrum'}})
    viewer.addSurface(py3Dmol.SES, {'opacity': 0.8, 'color': 'lightblue'})
    viewer.zoomTo()
    
    # 3. 生成PNG图像[1,13](@ref)
    png_data = viewer.png()
    with open(output_png, 'wb') as png_file:
        png_file.write(png_data)
    
    # 4. 生成PDF图像[1,3](@ref)
    with PdfPages(output_pdf) as pdf:
        img = Image.open(output_png)
        plt.figure(figsize=(10, 10 * img.height / img.width))
        plt.imshow(img)
        plt.axis('off')
        plt.title(f"Protein Structure: {os.path.basename(pdb_file)}")
        pdf.savefig(bbox_inches='tight')
        plt.close()
    
    print(f"成功生成图像: {output_png} 和 {output_pdf}")

if __name__ == "__main__":
    # 参数配置
    input_file = "1example.pdb"  # 替换为您的PDB文件路径
    output_png = "protein_structure.png"
    output_pdf = "protein_structure.pdf"
    
    # 执行可视化
    visualize_pdb(input_file, output_png, output_pdf)