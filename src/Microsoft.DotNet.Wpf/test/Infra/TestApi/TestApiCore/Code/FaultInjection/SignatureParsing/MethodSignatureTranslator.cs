// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Collections.Generic;

namespace Microsoft.Test.FaultInjection.SignatureParsing
{
    internal static class MethodSignatureTranslator
    {
        #region Public Members

        public static String GetGenericParaString(Type genericType, int genericIndex)
        {
            String strGenericParas = null;
            Type[] allGenericTypes = genericType.GetGenericArguments();


            int genericNumber = GetGenericNumber(genericType);

            if (allGenericTypes != null && allGenericTypes.Length > 0 && genericNumber > 0)
            {
                Type[] currentGenericTypes = new Type[genericNumber];

                strGenericParas = "<";
                for (int i = 0; i < genericNumber; ++i)
                {
                    currentGenericTypes[i] = allGenericTypes[genericIndex + i];
                }
                foreach (Type genericPara in currentGenericTypes)
                {
                    strGenericParas = strGenericParas.Insert(strGenericParas.Length, GetTypeString(genericPara) + ",");
                }
                if (currentGenericTypes.Length > 0)
                {
                    strGenericParas = strGenericParas.Remove(strGenericParas.Length - 1);
                }
                strGenericParas = strGenericParas.Insert(strGenericParas.Length, ">");
            }
            return strGenericParas;
        }

        public static int GetGenericNumber(Type type)
        {
            String[] temp = type.Name.Split('`');
            int genericParaNum = System.Int32.Parse(temp[1], CultureInfo.InvariantCulture);
            return genericParaNum;
        }

        public static String GetTypeString(Type type)
        {
            String methodString = null;
            Stack<Type> stack = new Stack<Type>();
            stack.Push(type);
            while (type.IsNested == true && !(type.IsGenericType == false && type.FullName == null)) //Eliminate <T>
            {
                type = type.DeclaringType;
                stack.Push(type);
            }
            
            Type outterType = stack.Pop();
            methodString = outterType.ToString().Replace('+','.');
            int genericIndex = 0;

            if (outterType.IsGenericType == true)
            {
                String[] temp = methodString.Split('[');
                methodString = temp[0];
                methodString = methodString.Remove(methodString.LastIndexOf('`'));
                methodString = methodString.Insert(methodString.Length, GetGenericParaString(outterType, genericIndex));
                genericIndex += GetGenericNumber(outterType);
            }
            while (stack.Count > 0)
            {
                Type currentType = stack.Pop();
                methodString = methodString.Insert(methodString.Length, "." + currentType.Name);

                if (currentType.IsGenericType == true && currentType.Name.Contains("`") == true)
                {
                    methodString = methodString.Remove(methodString.LastIndexOf('`'));
                    methodString = methodString.Insert(methodString.Length, GetGenericParaString(currentType, genericIndex));
                    genericIndex += GetGenericNumber(currentType);
                }
            }
            return methodString;
        }

        #endregion
    }
}