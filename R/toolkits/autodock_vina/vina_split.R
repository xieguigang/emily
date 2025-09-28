#' extract models by the autodock vina split
#' 
#' @param result_pdbqt the autodock vina docking output pqdqt file its file path.
#' @param scores a dataframe that contains thescore that parsed from the vina docking result log output text
#' @param prot_pdbqt the content text data of the protein receptor input of the vina docking.
#' 
const split_pdbqt = function(scores, result_pdbqt, prot_pdbqt = NULL) {
    let models = vina_split(result_pdbqt |> readText());
    
    if (nchar(prot_pdbqt) > 0) {
        # simply combine the pdbqt text data 
        # as the complex result
        models = `${models}${prot_pdbqt}`;
    }

    scores[,"pdbqt"] = models;    
    scores;
}