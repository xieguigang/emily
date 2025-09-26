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
    dir.create(temp_dir, recursive = TRUE);
    
    print("Processing of the molecule docking in temp workdir:");
    print(temp_dir);

    options(list.symbol_names = FALSE);

    let orig_wd <- getwd();
    
    on.exit({
        options(list.symbol_names = TRUE);
        setwd(orig_wd);
        
        if (make_cleanup) {
            unlink(temp_dir, recursive = TRUE);
        }
    }, add = TRUE);

    setwd(temp_dir);

    # 1. 准备受体（蛋白质）
    message("Preparing receptor (protein)...");
    let prot_pdbqt <- normalizePath(file.path(temp_dir, "protein.pdbqt"));
    # 使用pythonsh而不是python2
    let receptor_cmd <- list(
        prepare_receptor,
        "-r" = shQuote(prot_pdb),
        "-o" = shQuote(prot_pdbqt),
        "-A" = "hydrogens",
        "-U" = "nphs_lps_waters" # 删除非极性氢、配体和水分子
    );
    
    system2(pythonsh_bin, receptor_cmd, verbose = TRUE, shell = TRUE);

    print(`check processed protein model: ${prot_pdbqt}`);

    if (!file.exists(prot_pdbqt)) {
        stop("Failed to generate receptor PDBQT file: ", prot_pdbqt);
    }

    # 2. 准备配体（小分子）
    message("Preparing ligand...");
    let ligand_pdbqt <- normalizePath(file.path(temp_dir, "ligand.pdbqt"));
    let ligand_cmd <- list(
        prepare_ligand,
        "-l" = shQuote(ligand_pdb),
        "-o" = shQuote(ligand_pdbqt),
        "-A" = "hydrogens",
        "-U" = "nphs_lps" # 删除非极性氢和配体
    );
    
    system2(pythonsh_bin, ligand_cmd, verbose = TRUE, shell = TRUE);

    print(`check processed ligand model: ${ligand_pdbqt}`);

    if (!file.exists(ligand_pdbqt)) {
        stop("Failed to generate ligand PDBQT file: ", ligand_pdbqt);
    }

    let center_x, center_y, center_z = 0.0;

    # 3. 确定对接盒子中心
    if (is.null(center)) {
        message("Calculating docking box center as protein centroid...");        
        
        center = prot_pdb 
        |> read.pdb() 
        |> pdb_centroid(as.vector = TRUE)
        ;

        center_x <- center[1];
        center_y <- center[2];
        center_z <- center[3];
        
        message(sprintf("Calculated center: x=%.3f, y=%.3f, z=%.3f", center_x, center_y, center_z));
    } else {
        if (length(center) != 3) {
            stop("Parameter 'center' must be a numeric vector of length 3 (x, y, z).");
        }
        center_x <- center[1];
        center_y <- center[2];
        center_z <- center[3];
        message(sprintf("Using user-provided center: x=%.3f, y=%.3f, z=%.3f", center_x, center_y, center_z));
    }

    # 4. 创建配置文件
    message("Configuring docking box...");

    let config_file <- file.path(temp_dir, "config.txt");
    let config_lines <- c(
        paste("receptor =", prot_pdbqt),
        paste("ligand =", ligand_pdbqt),
        paste("center_x =", round(center_x, 4)),
        paste("center_y =", round(center_y, 4)),
        paste("center_z =", round(center_z, 4)),
        paste("size_x =", size[1]),
        paste("size_y =", size[2]),
        paste("size_z =", size[3]),
        paste("num_modes =", num_modes),
        paste("energy_range =", energy_range),
        paste("exhaustiveness =", exhaustiveness),
        paste("cpu =", cpu)
    );
    if (!is.null(seed)) {
        config_lines <- c(config_lines, paste("seed =", seed));
    }
    writeLines(config_lines, config_file);

    # 5. 运行AutoDock Vina对接
    message("Running AutoDock Vina...");
    let output_pdbqt <- normalizePath(file.path(temp_dir, "output.pdbqt"));
    let log_file <- normalizePath(file.path(temp_dir, "vina_log.txt"));

    # Input:
    # --receptor arg        rigid part of the receptor (PDBQT)
    # --flex arg            flexible side chains, if any (PDBQT)
    # --ligand arg          ligand (PDBQT)

    # Search space (required):
    # --center_x arg        X coordinate of the center
    # --center_y arg        Y coordinate of the center
    # --center_z arg        Z coordinate of the center
    # --size_x arg          size in the X dimension (Angstroms)
    # --size_y arg          size in the Y dimension (Angstroms)
    # --size_z arg          size in the Z dimension (Angstroms)

    # Output (optional):
    # --out arg             output models (PDBQT), the default is chosen based on
    #                         the ligand file name
    # --log arg             optionally, write log file

    # Misc (optional):
    # --cpu arg                 the number of CPUs to use (the default is to try to
    #                             detect the number of CPUs or, failing that, use 1)
    # --seed arg                explicit random seed
    # --exhaustiveness arg (=8) exhaustiveness of the global search (roughly
    #                             proportional to time): 1+
    # --num_modes arg (=9)      maximum number of binding modes to generate
    # --energy_range arg (=3)   maximum energy difference between the best binding
    #                             mode and the worst one displayed (kcal/mol)

    # Configuration file (optional):
    # --config arg          the above options can be put here

    # Information (optional):
    # --help                display usage summary
    # --help_advanced       display usage summary with advanced options
    # --version             display program version

    let vina_cmd <- list(
        "--receptor" = prot_pdbqt,
        "--ligand" = ligand_pdbqt,
        "--center_x" = center_x,
        "--center_y" = center_y,
        "--center_z" = center_z,
        "--size_x" = size[1],
        "--size_y" = size[2],
        "--size_z" = size[3],
        "--out" = output_pdbqt,
        "--log" = log_file,
        "--cpu" = cpu,
        "--num_modes" = num_modes,
        "--energy_range" = energy_range,
        "--exhaustiveness" = exhaustiveness
    );

    if (!is.null( seed)) {
        vina_cmd$"--seed" = seed;
    }

    system2(vina_exe, vina_cmd, verbose = TRUE, shell = TRUE);

    if (!file.exists(output_pdbqt)) {
        stop("AutoDock Vina did not generate the expected output file: ", output_pdbqt);
    }

    # 6. 处理对接结果
    message("Processing results...");

    let vina_score = readLines(log_file) 
        |> vina_score_parser(n = num_modes) 
        |> split_pdbqt(output_pdbqt)
        ;

    message("Molecular docking completed successfully.");

    return(vina_score);
}



