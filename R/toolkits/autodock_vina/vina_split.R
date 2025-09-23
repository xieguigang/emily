#' Commandline call of the autodock vina split
#' 
const vina_split = function(output_file, autodock_vina_dir = "/opt/autodock_vina") {
    let vina <- file.path(autodock_vina_dir, "bin", "vina_split");
    let args <- list("--input" = output_file);

    system2(vina, args, verbose = TRUE);

    
}