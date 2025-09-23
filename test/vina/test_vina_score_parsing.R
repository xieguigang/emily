require(emily);

let txt = readLines(relative_work("vina_log.txt"));
let score = emily::vina_score_parser(txt, n=10);

print(score);