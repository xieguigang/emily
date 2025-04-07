#' zdock workflow for docking two structure data
#' 
#' @param L the pdb file path 
#' @param R the another pdb file path
#' 
const zdock = function(L, R, outdir = "./") {
    let L1 = file.path(outdir, basename(L) & ".pdb");
    let R1 = file.path(outdir, basename(R) & ".pdb");
    let zdocklib = getOption("zdock");
    let zdock = file.path(zdocklib,"zdock");
    let zdock.out = file.path(outdir, "zdock.out");

    zdocklib |> mark_sur(L, L1);
    zdocklib |> mark_sur(R, R1);

    system2(zdock, list(
        "-R" = R1,
        "-L" = L1,
        "-o" = zdock.out 
    ));

    zdock.out;
}

const mark_sur = function(zdock, i, o) {
    system2(`${zdock}/mark_sur`, [i o]);
}