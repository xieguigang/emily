#!/usr/bin/env python
import sys
from rdkit import Chem
from rdkit.Chem import AllChem

def smiles_to_pdb(smiles, output_file = None):
    try:
        # 转换流程
        mol = Chem.MolFromSmiles(smiles)
        mol = Chem.AddHs(mol)  # 添加氢原子
        AllChem.EmbedMolecule(mol, randomSeed=0xf00d)  # 生成3D坐标
        AllChem.MMFFOptimizeMolecule(mol)  # 力场优化
        
        if output_file is None:
            print(Chem.MolToPDBBlock(mol))
        else:
            # 写入PDB文件
            Chem.MolToPDBFile(mol, output_file)
            print(f"Success: {smiles} → {output_file}")

        return 0
    except Exception as e:
        print(f"Error: {str(e)}")
        return 1

if __name__ == "__main__":
    
    if len(sys.argv) == 1:
        print("Usage: smiles2pdb.py <SMILES> [<OUTPUT.pdb>]")
        sys.exit(1)
    else:

        if len(sys.argv) == 2:
            _, smiles = sys.argv
            sys.exit(smiles_to_pdb(smiles, None))
        else:
            _, smiles, output = sys.argv
            sys.exit(smiles_to_pdb(smiles, output))