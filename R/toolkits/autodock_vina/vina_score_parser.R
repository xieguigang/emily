#' Parse AutoDock Vina Docking Score Text File
#' 
#' This function parses the output from AutoDock Vina, specifically the table
#' that contains docking scores for each mode, and converts it into a dataframe.
#' 
#' @param vina A character vector containing the content of a Vina score text file, 
#'   typically read via \code{\link{readLines}}.
#' @param n The value of the \code{num_modes} command-line argument used in the 
#'   Vina docking configuration, indicating how many modes were generated.
#' 
#' @return A dataframe with the parsed Vina scores. The dataframe has four columns:
#'   \describe{
#'     \item{affinity(kcal/mol)}{The binding affinity in kcal/mol for each mode.}
#'     \item{rmsd l.b.}{The RMSD lower bound for each mode compared to the best mode.}
#'     \item{rmsd u.b.}{The RMSD upper bound for each mode compared to the best mode.}
#'   }
#'   The row names are formatted as "#1", "#2", etc., corresponding to the mode numbers.
#' 
#' @details This function identifies the table in the Vina output that contains the 
#'   docking scores. AutoDock Vina is a widely used open-source program for 
#'   molecular docking, which predicts how small molecules bind to a receptor 
#'   of known 3D structure. The output includes multiple binding modes (poses) 
#'   with their corresponding scores.
#'   
#'   The table typically has the following structure:
#'   \preformatted{
#'   mode |   affinity | dist from best mode
#'        | (kcal/mol) | rmsd l.b.| rmsd u.b.
#'   -----+------------+----------+----------
#'     1         -6.6      0.000      0.000
#'     2         -6.2      2.396      9.931
#'     3         -6.2      1.562      2.369
#'     ...
#'   }
#'   The function extracts this table, processes each line to split on whitespace, 
#'   and converts the values to numeric types before creating the final dataframe.
#'   
#'   If the function cannot find the table header (a line starting with "mode   |"), 
#'   it will stop with an error message indicating that no model result could be parsed.
#' 
#' @examples
#' \dontrun{
#' # Read the Vina output file
#' vina_output <- readLines("vina_output.txt")
#' 
#' # Parse the scores, assuming 10 modes were requested
#' scores_df <- vina_score_parser(vina_output, n = 10)
#' 
#' # View the resulting dataframe
#' print(scores_df)
#' 
#' # Access the affinity of the best mode
#' best_affinity <- scores_df[1, "affinity(kcal/mol)"]
#' print(paste("Best affinity:", best_affinity, "kcal/mol"))
#' }
#' 
#' @note This function requires no additional packages beyond base R.
#' 
#' @seealso \code{\link{readLines}} for reading text files into R.
#' 
#' @references 
#' Trott O, Olson AJ. AutoDock Vina: improving the speed and accuracy 
#' of docking with a new scoring function, efficient optimization, and 
#' multithreading. J Comput Chem. 2010;31(2):455-461. 
#' doi:10.1002/jcc.21334
#' 
#' @author 
#' @export
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