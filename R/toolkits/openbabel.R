#' Convert the smiles structure information as pdb structure data
#' 
#' @param openbabel the program file path to the ``obabel``.
#' @return this function returns a plain text content data of the generated pdb structrue file data.
#' 
const smiles_2_pdb = function(smiles, db_xref = "NA", openbabel = getOption("openbabel")) {
    system2(openbabel, `-ismiles -:"${smiles}" --title "${db_xref}" --gen3d -opdb`);
}