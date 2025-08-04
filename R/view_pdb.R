imports "proteinKit" from "seqtoolkit";
imports "htmlReport" from "reportKit";
imports "pdf" from "reportKit";

const view_pdb = function(pdb_txt, save_file) {
    let title = parse_pdb(pdb_txt, safe = TRUE) |> toString();
    let viewer = system.file("data/viewer/index.html", package = "emily");
    let outputdir = dirname(save_file);
    let html = htmlReport::reportTemplate(dirname(viewer), copyToTemp=TRUE, tmpdir =outputdir) + list(
        title = title,
        pdb = pdb_txt
    );
    let pdb_id = basename(save_file);
    let htmlfile = file.path(outputdir, `${pdb_id}.html`);

    htmlReport::flush(html);
    file.rename(file.path(outputdir,"index.html"), htmlfile);

    return(htmlfile);
}

const pdb_pdf = function(pdb_txt, save_file) {
    let out_html = file.path(dirname(save_file), `${basename(save_file)}.html`);

    pdf::makePDF(emily::view_pdb(pdb_txt, out_html),
        pdfout = save_file,
        wwwroot = dirname(save_file),
        style = NULL,
        resolvedAsDataUri = FALSE,
        footer = NULL,
        header = NULL,
        opts = pdfGlobal_options(orientation="Landscape"),
        pageOpts = NULL,
        pdf_size = "A4"
    );
}