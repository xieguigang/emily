#' extract models by the autodock vina split
#' 
const split_pdbqt = function(scores, result_pdbqt) {
    let models = vina_split(result_pdbqt |> readText());
    
    scores[,"pdbqt"] = models;    
    scores;
}