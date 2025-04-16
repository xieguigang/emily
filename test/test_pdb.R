require(emily);


print(smiles_2_pdb(smiles = "COC1=C(C=C(C=C1)CC2COC(=O)C2CC3=CC(=C(C=C3)OC4C(C(C(C(O4)CO)O)O)O)OC)OC", db_xref = "NA", debug = TRUE));
print("test rdkit");
print(smiles_2_pdb_rdkit(smiles = "COC1=C(C=C(C=C1)CC2COC(=O)C2CC3=CC(=C(C=C3)OC4C(C(C(C(O4)CO)O)O)O)OC)OC", db_xref = "NA", debug = TRUE));