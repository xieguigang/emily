#' zdock workflow for docking two structure data
#' 
#' @param L the pdb file path 
#' @param R the another pdb file path
#' @param n set the number of output predictions, (default to 10)
#' @param fixed fix the receptor, preventing its rotation and switching with ligand during execution.
#' 
#' @return 
#' 
const zdock = function(L, R, outdir = "./", fixed = FALSE, n = 10) {
    let L1 = file.path(outdir, basename(L) & ".pdb");
    let R1 = file.path(outdir, basename(R) & ".pdb");
    let zdocklib = getOption("zdock");
    let zdock = file.path(zdocklib,"zdock");
    let mk_complex = file.path(zdocklib,"create.pl");
    let create_lig = file.path(zdocklib,"create_lig");
    let zdock.out = "./zdock.out";
    let workdir = getwd();

    # Standard PDB format files must be processed by mark_sur to add atom radius, 
    # charge and atom type. If you know that some atoms are not in the binding site, 
    # you can block them by changing their atom type (column 55-56) to 19.
    zdocklib |> mark_sur(L, L1);
    zdocklib |> mark_sur(R, R1);

    setwd(outdir);
    file.copy(create_lig, outdir & "/");

    # argument options should keeps the input order
    # Usage: zdock [options] -R [receptor.pdb] -L [ligand.pdb]
    system2(zdock, list(
        "-F" = fixed,
        "-N" = n,
        "-o" = zdock.out, 
        "-R" = R1,
        "-L" = L1        
    ));

    if (file.exists(zdock.out)) {
        system2(mk_complex, zdock.out);

        let complex.1.pdb = readText("./complex.1.pdb");
        let zdocking = readText(zdock.out);
        setwd(workdir);
        list(zdock = zdocking, pdb_txt = complex.1.pdb);
    } else {
        setwd(workdir);
        invisible(NULL);
    }
}

const mark_sur = function(zdock, i, o) {
    system2(`${zdock}/mark_sur`, [i o]);
}