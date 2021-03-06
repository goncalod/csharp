﻿using CodeToUMLNotation.ModelV2;
using CodeToUMLNotation.ModelV2.Abstract;
using CodeToUMLNotation.ModelV2.Code;
using CodeToUMLNotation.NRefactoryHelper.Interfaces;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using al = CodeToUMLNotation.NRefactoryHelper.Interfaces.NRefactoryVisitorV2DeclarationHelper;

namespace CodeToUMLNotation.NRefactoryHelper
{
    public class NRefactoryVisitorV2 : DepthFirstAstVisitor
    {
        public IEnumerable<Declaration> Declarations { 
            get { return m_declarations.Value.ToList(); }
        }


        private readonly Lazy<LinkedList<Declaration>> m_declarations = new Lazy<LinkedList<Declaration>>(() => new LinkedList<Declaration>());    

        // Fields there is only on Classes or Structs
        public override void VisitFieldDeclaration(FieldDeclaration fd)
        {
            ClassesAndStructs cs = (ClassesAndStructs)GetDeclarationFor(fd);
            
            string name = fd.Variables.Single().Name;
            string returnType = fd.ReturnType.ToString();
            Visibility v = new Visibility(VisibilityMapper.Map(fd.Modifiers));

            if (al.CheckFlag(fd.Modifiers, Modifiers.Const))
            {
                Constant cf = new Constant(v, name, returnType, fd.Variables.Single().Initializer.ToString());
                cs.ConstantFields.Add(cf);
            }

            else
            {
                Field f = new Field(v, name,
                     al.CheckFlag(fd.Modifiers, Modifiers.Static),
                     al.CheckFlag(fd.Modifiers, Modifiers.Readonly),
                     returnType
                );

                cs.Fields.Add(f);
            }
            AddToNotDefaultReferencedTypes(cs, returnType);

            // call base to forward execution
            base.VisitFieldDeclaration(fd);
        }

        // Properties there is only on Classes or Structs or Interfaces
        public override void VisitPropertyDeclaration(PropertyDeclaration pd)
        {
            ClassesAndStructsAndInterfaces csi = (ClassesAndStructsAndInterfaces)GetDeclarationFor(pd);
            string returnType = pd.ReturnType.ToString();

            Property p = new Property(
                GetVisibility(pd),
                pd.Name,
                al.CheckFlag(pd.Modifiers, Modifiers.Static),
                al.CheckFlag(pd.Modifiers, Modifiers.Virtual),
                al.CheckFlag(pd.Modifiers, Modifiers.Abstract),
                al.CheckFlag(pd.Modifiers, Modifiers.Override),
                returnType,
                pd.Getter.HasChildren,
                pd.Setter.HasChildren
            );


            csi.Properties.Add(p);
            AddToNotDefaultReferencedTypes(csi, returnType);

            // call base to forward execution
            base.VisitPropertyDeclaration(pd);
        }

        // Constructors there is only on Classes or Structs
        public override void VisitConstructorDeclaration(ConstructorDeclaration cd)
        {
            AddMethod(true, cd, cd.Parameters);

            // call base to forward execution
            base.VisitConstructorDeclaration(cd);
        }

        // Methods there is only on Classes or Structs or Interfaces
        public override void VisitMethodDeclaration(MethodDeclaration md)
        {
            ClassesAndStructsAndInterfaces csi = (ClassesAndStructsAndInterfaces)GetDeclarationFor(md);
            AddMethod(false, md, md.Parameters);
            string returnType = md.ReturnType.ToString();
            AddToNotDefaultReferencedTypes(csi, returnType);

            // call base to forward execution
            base.VisitMethodDeclaration(md);
        }

        public override void VisitEnumMemberDeclaration(EnumMemberDeclaration ed)
        {
            ModelV2.Code.Enum e = (ModelV2.Code.Enum) GetDeclarationFor(ed);

            string[] keyValue = ed.GetText().Split('=');
            e.Values.Add(new KeyValuePair<string,string>(keyValue[0], (keyValue.Length == 2) ? keyValue[1] : ""));

            // call base
            base.VisitEnumMemberDeclaration(ed);
        }

        public override void VisitTypeDeclaration(TypeDeclaration td)
        {
            Declaration d = NRefactoryVisitorV2DeclarationHelper.Converters.Single(x => x.Handle(td.ClassType)).Create(td);
            m_declarations.Value.AddLast(d);

            // call base
            base.VisitTypeDeclaration(td);
        }



        #region Helpers


        private Visibility GetVisibility(EntityDeclaration ed)
        {
            if (m_declarations.Value.Count == 0)
                throw new InvalidOperationException();

            Interface i = m_declarations.Value.Last.Value as Interface;

            if (i == null)
            {
                // use normal
                return new Visibility(VisibilityMapper.Map(ed.Modifiers));
            }
            else
            {
                // adjust
                if ((ed.Modifiers & Modifiers.Internal) == Modifiers.Internal)
                    return new Visibility(ModelV2.Enums.VisibilityMode.@internal);
                else
                    return new Visibility(ModelV2.Enums.VisibilityMode.@public);
            }
        }


        private Declaration GetDeclarationFor(EntityDeclaration entity)
        {
            if (m_declarations.IsValueCreated == false)
                return null;
            // GetNameForGenericTypeDeclaration
            TypeDeclaration td = entity.Parent as TypeDeclaration;
            if (td == null)
                throw new InvalidOperationException("Parent is not a declaration");

            return m_declarations.Value.Last(x => x.Name == NRefactoryVisitorV2DeclarationHelper.GetNameForGenericTypeDeclaration(td));
        }



        private static bool AddToNotDefaultReferencedTypes(ClassesAndStructsAndInterfaces csi, string type)
        {
            ParameterValidator.ThrowIfArgumentNullOrEmpty(type, "type");
            IEnumerable<string> systemTypes = typeof(bool).Assembly.GetTypes().Where(x => !string.IsNullOrEmpty(x.Namespace) && x.Namespace.StartsWith("System")).Select(x => x.Name.ToLower());
            string typeLower = type.ToLower();

            typeLower = AdjustTypeMismatch(typeLower);

            if (!csi.ReferencedTypes.Contains(type) && !systemTypes.Contains(typeLower) && !type.StartsWith("ICSharpCode"))
            {
                csi.ReferencedTypes.Add(type);
                return true;
            }
            return false;
        }

        private static string AdjustTypeMismatch(string typeLower)
        {
            ParameterValidator.ThrowIfArgumentNullOrEmpty(typeLower, "typeLower");

            if (typeLower.Last() == '?')
                typeLower = typeLower.Substring(0, typeLower.Length - 1);

            if (typeLower == "bool")
                typeLower = "boolean";
            
            if (typeLower == "int")
                typeLower = "int32";

            return typeLower;
        }

        



        private void AddMethod(bool ctor, EntityDeclaration md, AstNodeCollection<ParameterDeclaration> ps)
        {
            ClassesAndStructsAndInterfaces csi = (ClassesAndStructsAndInterfaces)GetDeclarationFor(md);

            List<KeyValuePair<string, string>> args = ps.Select(p =>
                                                        new KeyValuePair<string, string>(p.Name, p.Type.ToString())
                                                      )
                                                      .ToList();

            if( args.Count > 0 ){
                args.ForEach(kvp => AddToNotDefaultReferencedTypes(csi, kvp.Value));
            }


            Method m = new Method(
                GetVisibility(md),
                md.Name,
                al.CheckFlag(md.Modifiers, Modifiers.Static),
                al.CheckFlag(md.Modifiers, Modifiers.Virtual),
                al.CheckFlag(md.Modifiers, Modifiers.Abstract),
                al.CheckFlag(md.Modifiers, Modifiers.Override),
                md.ReturnType.ToString(),
                ctor,                
                args
            );

            AddToNotDefaultReferencedTypes(csi, m.ReturnType);
            csi.Methods.Add(m);
        }


        #endregion

    }
}
