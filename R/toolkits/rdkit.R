#' needs conda environment and have rdkit installed
#' 
const smiles_2_pdb_rdkit = function(smiles, db_xref = "NA", rdkit = getOption("rdkit_py"), debug = FALSE) {
    let .tool = system.file("scripts/smiles2pdb.py", package = "emily");
    let pdb_cli = [.tool smiles];
    let pdb_txt = system2(rdkit, pdb_cli);

    pdb_txt;
}