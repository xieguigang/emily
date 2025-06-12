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
     * @param size default value Is ``'3000,2100'``.
   */
   function draw_pdb(pdb: object, size?: any): any;
   /**
    * parse the zdock output text
    * 
    * 
     * @param str -
   */
   function parse_zdock(str: string): object;
}
