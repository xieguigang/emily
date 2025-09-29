require(emily);

setwd(relative_work());

let i = 0;
let result = emily::autodock_vina(prot_pdb = "./r.pdb", ligand_pdb = "./l.pdb", 
                               center = NULL,
                               size = c(25.0, 25.0, 25.0),
                               num_modes = 10,
                               energy_range = 4,
                               exhaustiveness = 8,
                               cpu = 32,
                               seed = NULL,
                               mgltools_dir = "/opt/mgltools", 
                               autodock_vina_dir = "/opt/autodock_vina", 
                               temp_dir = "./tmp",
                               make_cleanup = FALSE);
str(result);

for(let model in as.list(result,byrow=TRUE)) {
    writeLines(model$pdbqt, con = `./output/model_${i=i+1}.pdbqt`);

    model = parse_pdb(model$pdbqt);

    pdf(file = `./output/model_${i-1}.pdf`) {
        draw_ligand2D(model, size = [4000,3000], dpi = 300);
    }
}

write.csv(result, file = "./output/docking.csv");