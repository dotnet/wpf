//------------------------------------------------------------------------------
//

//
// Description: Schema resource generator
//
//
//
//
//---------------------------------------------------------------------------

namespace MS.Internal.SchemaGen.Main
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Collections.Generic;
    using System.Resources;
    using System.Xml;

    public class SchemaGen
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        private SchemaGen()
        {
        }
        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods
        //+-----------------------------------------------------------------------------
        //
        //  Member:    Main
        //
        //  Synopsis:  The Main method for the SchemaGen project.
        //             Does the following:
        //
        //             1. Read the given XSD files.
        //             2. Writes out a compiled resource file
        //
        //------------------------------------------------------------------------------

        public static void Main(string[] args)
        {
//            System.Diagnostics.Debugger.Break();
            if (args.GetLength(0)< 4 )
            {
                Console.WriteLine("Usage: SchemaGen.exe <target.resource> [DEFATTR <name>]+ [<resourcename> <master.xsd> <publish.xsd> <validate.xsd>]+");
                Console.WriteLine("DEFATTR <name>   Validating schema will generate default attribute values if missing from XML markup.");
                return;
            }

            ResourceWriter resourceWriter = new ResourceWriter(args[0]);
            int idx = 1;
            ArrayList defAttr = new ArrayList();
            while ( args[idx].Equals("DEFATTR"))
            {
                idx++;
                defAttr.Add(args[idx]);
                idx++;
            }
            
            for (int i = idx; i < args.GetLength(0); i += 4)
            {
                string resourceName = args[i];
                string masterName= args[i+1];
                string publishName = args[i+2];
                string validateName = args[i+3];

                if (MakeDerived(masterName, publishName, validateName, defAttr))
                {
                    Stream validate = File.OpenRead(validateName);

                    byte[] data = new byte[validate.Length];
                    validate.Read(data, 0, (int)validate.Length);
                    validate.Close();

                    resourceWriter.AddResource(resourceName, data);
                }
            }

            resourceWriter.Generate();
            resourceWriter.Close();
            
            Console.WriteLine("Schema resource gen done.");
        }
        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------


        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private static XmlNode FindElement(XmlNode node, string name)
        {
            if (node == null)
            {
                return null;
            }
            if (node.NodeType == XmlNodeType.Element && node.Name == name)
            {
                return node;
            }
            foreach (XmlNode child in node.ChildNodes)
            {
                if ((node = FindElement(child, name)) != null)
                {
                    return node;
                }
            }
            return null;
        }

        private static XmlNode FindAttributeWithDefault(XmlNode node, ArrayList defAttr)
        {
            if (node == null)
            {
                return null;
            }
            if (node.NodeType == XmlNodeType.Element && ((XmlElement)node).Attributes.GetNamedItem("default")!= null)
            {
                bool preserve = false;
                foreach ( string attr in defAttr )
                {
                    string name;
                    name = ((XmlElement)node).Attributes.GetNamedItem("name").Value;
                    if ( name!= null && name.Equals(attr))
                    {
                        preserve= true;
                        break;
                    }
                }
                if ( !preserve )
                {
                    return node;
                }
            }
            foreach (XmlNode child in node.ChildNodes)
            {
                if ((node = FindAttributeWithDefault(child, defAttr)) != null)
                {
                    return node;
                }
            }
            return null;
        }

        private static bool MakeDerived(string master,string publish,string validate,ArrayList defAttr)
        {
            XmlDocument schema = new XmlDocument();
            schema.PreserveWhitespace = true;

            try
            {
                schema.Load(master);
            }
            catch (Exception e)
            {
                Console.WriteLine("Schema: " + master);
                Console.WriteLine(e.Message);
                return false;
            }

            XmlNode node;
            if ( (node = FindElement(schema,"xs:schema"))!= null )
            {
                XmlAttribute remove = null;
                foreach ( XmlAttribute check in node.Attributes )
                {
                    if ( check.Name == "xmlns:xml" )
                    {
                        remove = check;
                        break;
                    }
                }

                if ( remove!= null )
                {
                    node.Attributes.Remove(remove);
                }
            }

            if ( (node = FindElement(schema,"xs:import"))!= null )
            {
                XmlNode parent = node.ParentNode;

                foreach ( XmlNode check in parent.ChildNodes )
                {
                    if ( check.Attributes!=null )
                    {
                        XmlAttribute remove = null;
                        foreach ( XmlAttribute attr in check.Attributes )
                        {
                            if ( attr.Name=="schemaLocation" )
                            {
                                remove = attr;
                                break;
                            }
                        }
                        if ( remove!=null )
                        {
                            check.Attributes.Remove(remove);
                        }
                    }
                }
            }
                
            while ( (node = FindElement(schema,"xs:annotation"))!= null )
            {
                if ( node.NextSibling is XmlWhitespace )
                {
                    node.ParentNode.RemoveChild(node.NextSibling);
                }
                node.ParentNode.RemoveChild(node);
            }

            schema.Save(publish);
            
            while ( (node = FindAttributeWithDefault(schema,defAttr))!= null )
            {
                if (node.NodeType == XmlNodeType.Element && ((XmlElement)node).Attributes.GetNamedItem("default")!= null)
                {
                    ((XmlElement)node).Attributes.RemoveNamedItem("default");
                }                    
            }
            schema.Save(validate);

            return true;
        }

        #endregion Private Methods
    }
}

