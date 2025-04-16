# imports "Rscript" from "emily";

const .onLoad = function() {
    # set directory path that contaains zdock program
    # config default docker environment.
    options(zdock = "/usr/local/bin/");

    # config openbabel
    if ((Sys.info()[['sysname']]) == "Win32NT") {
        options(openbabel = "C:\Program Files\OpenBabel-3.1.1\obabel.exe");
    }
}