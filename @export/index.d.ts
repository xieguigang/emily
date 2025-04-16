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
   */
   function mark_sur(zdock: any, i: any, o: any): object;
   /**
     * @param db_xref default value Is ``NA``.
     * @param openbabel default value Is ``Call "getOption"("openbabel")``.
   */
   function smiles_2_pdb(smiles: any, db_xref?: any, openbabel?: any): object;
   /**
     * @param outdir default value Is ``./``.
   */
   function zdock(L: any, R: any, outdir?: any): object;
}
