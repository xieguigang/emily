#' Helper function for parse autodock vina docking score text file as dataframe.
#' 
#' @param vina a character vector of the vina score text file content data which is read via readLines
#' @param n num_modes commandline argument value for the vina docking config.
#' 
#' @return a dataframe of the vina score parsed result.
#' 
const vina_score_parser = function(vina, n) {
    let offset = vina |> startsWith("mode\s+\|", fixed = FALSE); 
    # get line offset which is the table header line
    offset = which(offset);

    if (length(offset) == 0) {
        stop("no model result could be parsed from the result summary file!");
    }

    let ends = offset + n + 2;
    let score_table = vina[(offset+3):ends];

    # mode |   affinity | dist from best mode
    #      | (kcal/mol) | rmsd l.b.| rmsd u.b.
    # -----+------------+----------+----------
    #  1         -6.6      0.000      0.000
    #  2         -6.2      2.396      9.931
    #  3         -6.2      1.562      2.369
    #  4         -6.2     18.633     20.712
    #  5         -6.0     13.549     17.547
    #  6         -6.0     19.151     21.021
    #  7         -6.0     19.641     21.566
    #  8         -6.0      5.171      7.553
    #  9         -5.9     12.712     15.830
    #  10        -5.8     20.012     22.990
    score_table <- lapply(score_table, function(line_str) {
        # split current text line by whitespace
        # and cast to a numeric vector
        line_str 
        |> strsplit("\s+", fixed = FALSE) 
        |> skip(1) 
        |> as.numeric()
        ;
    });
    score_table = data.frame(
        row.names = sprintf("#%s", score_table@{1}), # column 1 is mode
        "affinity(kcal/mol)" = score_table@{2},      # column 2
        "rmsd l.b." = score_table@{3},               # column 3 rmsd lower bound
        "rmsd u.b." = score_table@{4}                # column 4 rmsd upper bound
    );

    return(score_table);
}