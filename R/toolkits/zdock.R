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
    let zdock.out = file.path(outdir, "zdock.out");

    # Standard PDB format files must be processed by mark_sur to add atom radius, 
    # charge and atom type. If you know that some atoms are not in the binding site, 
    # you can block them by changing their atom type (column 55-56) to 19.
    zdocklib |> mark_sur(L, L1);
    zdocklib |> mark_sur(R, R1);

    # argument options should keeps the input order
    # Usage: zdock [options] -R [receptor.pdb] -L [ligand.pdb]
    system2(zdock, list(
        "-F" = fixed,
        "-N" = n,
        "-o" = zdock.out, 
        "-R" = R1,
        "-L" = L1        
    ));

    zdock.out;
}

const mark_sur = function(zdock, i, o) {
    system2(`${zdock}/mark_sur`, [i o]);
}