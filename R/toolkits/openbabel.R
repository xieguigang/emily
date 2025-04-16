#' Convert the smiles structure information as pdb structure data
#' 
#' @param openbabel the program file path to the ``obabel``.
#' @return this function returns a plain text content data of the generated pdb structrue file data.
#' 
const smiles_2_pdb = function(smiles, db_xref = "NA", openbabel = getOption("openbabel"), debug = FALSE) {
    let pdb_cli = `-ismiles -:"${smiles}" --title "${db_xref}" --gen3d -opdb`;
    let pdb_txt = system2(openbabel, pdb_cli);
    # let data       = textlines(txt);
    # let strip_last = length(data) - 1;

    if (debug) {
        print(`obabel ${pdb_cli}`);

        if (nchar(pdb_txt) > 0) {
            cat("1 molecule converted:");
            cat("\n");
            cat(pdb_txt);
            cat("\n\n");
        } else {
            print("smiles struct data convert to pdb struct data failure!");
        }
    }

    # removes the last line
    # 1 molecule converted
    # paste(data[1:strip_last], sep = "\n");
    pdb_txt;
}