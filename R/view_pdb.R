imports "proteinKit" from "seqtoolkit";
imports "htmlReport" from "reportKit";

const view_pdb = function(pdb_txt, save_file) {
    let title = parse_pdb(pdb_txt, safe = TRUE) |> toString();
    let viewer = system.file("data/viewer/index.html", package = "emily");
    let outputdir = dirname(save_file);
    let html = htmlReport::reportTemplate(dirname(viewer), copyToTemp=TRUE, tmpdir =outputdir) + list(
        title = title,
        pdb = pdb_txt
    );

    htmlReport::flush(html);
}