#' Run Molecular Docking with AutoDock Vina
#'
#' This function performs protein-ligand molecular docking using AutoDock Vina
#' on a Linux system. It handles preparation of receptor and ligand PDB files
#' into PDBQT format, configuration of the docking box, execution of Vina,
#' and extraction of the top-scoring pose.
#'
#' @param prot_pdb Character string. Path to the receptor protein PDB file.
#' @param ligand_pdb Character string. Path to the ligand molecule PDB file.
#' @param center Numeric vector of length 3. (x, y, z) coordinates for the center of the docking box in Angstroms. If NULL (default), the centroid of the protein is used. Providing explicit coordinates is strongly recommended for accurate docking[1,7](@ref).
#' @param size Numeric vector of length 3. Size of the docking box in Angstroms (x, y, z dimensions). Default: c(25, 25, 25)
#' @param num_modes Integer. Maximum number of binding modes to generate. Default: 10
#' @param energy_range Numeric. Energy range (in kcal/mol) above the best mode to output. Default: 4
#' @param exhaustiveness Integer. Exhaustiveness of the global search. Higher values may improve results but increase computation time. Typical range: 8-32[9](@ref). Default: 8
#' @param cpu Integer. Number of CPU cores to use. Default: 1
#' @param seed Integer. Random seed. Use for reproducible results. Default: NULL (uses Vina default)
#' @param mgltools_dir Character string. Path to the MGLTools directory. Default: '/opt/mgltools'
#' @param autodock_vina_dir Character string. Path to the AutoDock Vina directory. Default: '/opt/autodock'
#' @param make_cleanup Logical. Should temporary files be removed after completion? Default: FALSE
#'
#' @return Invisible NULL. The function writes two output files: the top-scoring complex structure (`complex_pdb`) and a text file containing docking scores (`score_txt`).
#'
#' @details
#' The function performs the following steps:
#' \enumerate{
#'   \item Prepares the receptor PDB file into PDBQT format using `prepare_receptor4.py` from MGLTools.
#'   \item Prepares the ligand PDB file into PDBQT format using `prepare_ligand4.py` from MGLTools.
#'   \item Estimates the docking box center (if not provided) by calculating the centroid of all protein atoms.
#'   \item Writes a Vina configuration file.
#'   \item Executes AutoDock Vina.
#'   \item Parses the output to extract the top-scoring pose and saves it as a PDB file.
#'   \item Saves the docking scores to a text file.
#' }
#'
#' @note
#' \itemize{
#'   \item **Important:** Ensure MGLTools and AutoDock Vina are correctly installed on the system.
#'   \item The default method for determining the docking box center (protein centroid) is simplistic. For accurate results, especially with large proteins, provide explicit `center` coordinates based on known binding site information[1,7](@ref).
#'   \item The output PDB file for the complex (`complex_pdb`) is generated from the Vina output PDBQT. It may contain only the ligand atoms if the receptor was not included in the output. Use Vina's `--out` parameter configuration if receptor output is needed.
#'   \item Temporary files are created in a temporary directory unless `make_cleanup = FALSE`.
#' }
#'
#' @examples
#' \dontrun{
#' # Using protein centroid as box center (not always recommended)
#' autodock_vina(
#'   prot_pdb = "protein.pdb",
#'   ligand_pdb = "ligand.pdb"
#' )
#'
#' # Providing explicit box center coordinates (recommended)
#' autodock_vina(
#'   prot_pdb = "protein.pdb",
#'   ligand_pdb = "ligand.pdb",
#'   center = c(10.5, 22.3, 18.7), # Coordinates based on known binding site
#'   size = c(20, 20, 20),
#'   exhaustiveness = 16,
#'   cpu = 4
#' )
#' }
#'
#' @references
#' For more information on AutoDock Vina and its parameters, see:
#' \itemize{
#'   \item The AutoDock Vina documentation: \url{http://vina.scripps.edu/}
#'   \item MGLTools documentation: \url{https://ccsb.scripps.edu/mgltools/}
#' }
#'
#' @export
#' @family molecular docking
const autodock_vina = function(prot_pdb, ligand_pdb, 
                               center = NULL,
                               size = c(25.0, 25.0, 25.0),
                               num_modes = 10,
                               energy_range = 4,
                               exhaustiveness = 8,
                               cpu = 1,
                               seed = NULL,
                               mgltools_dir = "/opt/mgltools", 
                               autodock_vina_dir = "/opt/autodock_vina", 
                               temp_dir = tempfile("vina_dock_"),
                               make_cleanup = FALSE) {

    # 构建MGLTools实用工具脚本的完整路径
    let mgltools_utils_dir <- file.path(mgltools_dir, "MGLToolsPckgs/AutoDockTools/Utilities24");
    let prepare_receptor <- file.path(mgltools_utils_dir, "prepare_receptor4.py");
    let prepare_ligand <- file.path(mgltools_utils_dir, "prepare_ligand4.py");
    let pythonsh_bin <- file.path(mgltools_dir, "bin", "pythonsh");

    # 定义Vina可执行文件的路径
    let vina_exe <- file.path(autodock_vina_dir, "bin", "vina");

    # 检查必要工具是否存在
    if (!file.exists(pythonsh_bin)) {
        stop("MGLTools pythonsh not found at: ", pythonsh_bin);
    }
    if (!file.exists(prepare_receptor)) {
        stop("MGLTools prepare_receptor4.py script not found at: ", prepare_receptor);
    }
    if (!file.exists(prepare_ligand)) {
        stop("MGLTools prepare_ligand4.py script not found at: ", prepare_ligand);
    }
    if (!file.exists(vina_exe)) {
        stop("AutoDock Vina executable not found at: ", vina_exe);
    }
    if (!file.exists(prot_pdb)) {
        stop("Receptor PDB file not found: ", prot_pdb);
    }
    if (!file.exists(ligand_pdb)) {
        stop("Ligand PDB file not found: ", ligand_pdb);
    }
    # 为临时文件创建唯一的工作目录
    dir.create(temp_dir, recursive = TRUE,showWarnings=FALSE);
    
    print("Processing of the molecule docking in temp workdir:");
    print(temp_dir);

    let bash = system.file("data/vina.sh", package = "emily") 
    |> readText() 
    |> gsub("{$prot_pdb}", normalizePath(prot_pdb))
    |> gsub("{$ligand_pdb}", normalizePath(ligand_pdb))
    |> gsub("{$temp_dir}", normalizePath(temp_dir))
    |> gsub("{$cpu}", cpu)
    |> gsub("{$num_modes}", num_modes)
    ;

    writeLines(bash, con = file.path(temp_dir,"run.sh"));
    system2("/bin/bash", list(
        "-c" = file.path(temp_dir,"run.sh")) , shell = TRUE);

    # 6. 处理对接结果
    message("Processing results...");

    let log_file = file.path(temp_dir,"vina.log");
    let output_pdbqt = file.path(temp_dir,"output.pdbqt");
    let vina_score = readLines(log_file) 
        |> vina_score_parser(n = num_modes) 
        |> split_pdbqt(output_pdbqt)
        ;

    message("Molecular docking completed successfully.");

    return(vina_score);
}



