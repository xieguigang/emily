// export R# source type define for javascript/typescript language
//
// package_source=emily

declare namespace emily {
   module _ {
      /**
      */
      function onLoad(): object;
   }
   /**
     * @param complex_pdb default value Is ``./complex.pdb``.
     * @param score_txt default value Is ``./score.txt``.
     * @param center default value Is ``null``.
     * @param size default value Is ``Call "c"(25, 25, 25)``.
     * @param num_modes default value Is ``10``.
     * @param energy_range default value Is ``4``.
     * @param exhaustiveness default value Is ``8``.
     * @param cpu default value Is ``1``.
     * @param seed default value Is ``null``.
     * @param mgltools_dir default value Is ``/opt/mgltools``.
     * @param autodock_vina_dir default value Is ``/opt/autodock_vina``.
     * @param make_cleanup default value Is ``false``.
   */
   function autodock_vina(prot_pdb: any, ligand_pdb: any, complex_pdb?: any, score_txt?: any, center?: any, size?: any, num_modes?: any, energy_range?: any, exhaustiveness?: any, cpu?: any, seed?: any, mgltools_dir?: any, autodock_vina_dir?: any, make_cleanup?: any): object;
   /**
   */
   function mark_sur(zdock: any, i: any, o: any): object;
   /**
   */
   function pdb_pdf(pdb_txt: any, save_file: any): object;
   /**
     * @param db_xref default value Is ``NA``.
     * @param openbabel default value Is ``Call "getOption"("openbabel")``.
     * @param debug default value Is ``false``.
   */
   function smiles_2_pdb(smiles: any, db_xref?: any, openbabel?: any, debug?: any): object;
   /**
     * @param db_xref default value Is ``NA``.
     * @param rdkit default value Is ``Call "getOption"("rdkit_py")``.
     * @param debug default value Is ``false``.
   */
   function smiles_2_pdb_rdkit(smiles: any, db_xref?: any, rdkit?: any, debug?: any): object;
   /**
   */
   function view_pdb(pdb_txt: any, save_file: any): object;
   /**
     * @param outdir default value Is ``./``.
     * @param fixed default value Is ``false``.
     * @param n default value Is ``10``.
   */
   function zdock(L: any, R: any, outdir?: any, fixed?: any, n?: any): object;
}
