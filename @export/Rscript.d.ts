// export R# package module type define for javascript/typescript language
//
//    imports "Rscript" from "Emily";
//
// ref=Emily.Rscript@Emily, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null

/**
 * 
*/
declare namespace Rscript {
   /**
    * 
    * 
     * @param pdb -
     * @param ligand this function will treated the given **`pdb`** object as vina dock
     *  pdbqt object if this ligand reference is missing.
     * 
     * + default value Is ``null``.
     * @param style -
     * 
     * + default value Is ``null``.
     * @param size -
     * 
     * + default value Is ``'3600,2400'``.
     * @param dpi -
     * 
     * + default value Is ``120``.
     * @param env -
     * 
     * + default value Is ``null``.
   */
   function draw_ligand2D(pdb: object, ligand?: object, style?: object, size?: any, dpi?: object, env?: object): any;
   /**
     * @param size default value Is ``'3000,2100'``.
   */
   function draw_pdb(pdb: object, size?: any): any;
   /**
   */
   function parse_style(jsonstr: string): object;
   /**
    * parse the zdock output text
    * 
    * 
     * @param str -
   */
   function parse_zdock(str: string): object;
   /**
   */
   function vina_split(dock_pdbqt: string): string;
}
