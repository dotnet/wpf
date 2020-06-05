// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;


#pragma warning disable 0169 // warning CS0169: The field '...' is never used
#pragma warning disable 0649 // warning CS0169: Field '...' is never assigned to

namespace MS.Internal.WindowsRuntime
{
namespace Windows.Data.Text
{
    [global::WinRT.WindowsRuntimeType]
    
    internal enum AlternateNormalizationFormat : int
    {
        NotNormalized = unchecked((int)0),
        Number = unchecked((int)0x1),
        Currency = unchecked((int)0x3),
        Date = unchecked((int)0x4),
        Time = unchecked((int)0x5),
    }
    [global::WinRT.WindowsRuntimeType]
    [global::WinRT.ProjectedRuntimeClass(nameof(_default))]
    internal sealed class AlternateWordForm : global::System.Runtime.InteropServices.ICustomQueryInterface, IEquatable<AlternateWordForm>
    {
        public IntPtr ThisPtr => _default.ThisPtr;

        private IObjectReference _inner = null;
        private readonly Lazy<global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.IAlternateWordForm> _defaultLazy;

        private global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.IAlternateWordForm _default => _defaultLazy.Value;

        public static AlternateWordForm FromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero) return null;
            var obj = MarshalInspectable.FromAbi(thisPtr);
            return obj is AlternateWordForm ? (AlternateWordForm)obj : new AlternateWordForm((global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.IAlternateWordForm)obj);
        }

        public AlternateWordForm(global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.IAlternateWordForm ifc)
        {
            _defaultLazy = new Lazy<global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.IAlternateWordForm>(() => ifc);
        }

        public static bool operator ==(AlternateWordForm x, AlternateWordForm y) => (x?.ThisPtr ?? IntPtr.Zero) == (y?.ThisPtr ?? IntPtr.Zero);
        public static bool operator !=(AlternateWordForm x, AlternateWordForm y) => !(x == y);
        public bool Equals(AlternateWordForm other) => this == other;
        public override bool Equals(object obj) => obj is AlternateWordForm that && this == that;
        public override int GetHashCode() => ThisPtr.GetHashCode();

        private  IObjectReference GetDefaultReference<T>() => _default.AsInterface<T>();
        private  IObjectReference GetReferenceForQI() => _inner ?? _default.ObjRef;

        private struct InterfaceTag<I>{};

        private IAlternateWordForm AsInternal(InterfaceTag<IAlternateWordForm> _) => _default;

        public string AlternateText => _default.AlternateText;

        public AlternateNormalizationFormat NormalizationFormat => _default.NormalizationFormat;

        public TextSegment SourceTextSegment => _default.SourceTextSegment;

        private bool IsOverridableInterface(Guid iid) => false;

        global::System.Runtime.InteropServices.CustomQueryInterfaceResult global::System.Runtime.InteropServices.ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
        {
            ppv = IntPtr.Zero;
            if (IsOverridableInterface(iid) || typeof(global::WinRT.IInspectable).GUID == iid)
            {
                return global::System.Runtime.InteropServices.CustomQueryInterfaceResult.NotHandled;
            }

            if (GetReferenceForQI().TryAs<IUnknownVftbl>(iid, out ObjectReference<IUnknownVftbl> objRef) >= 0)
            {
                using (objRef)
                {
                    ppv = objRef.GetRef();
                    return global::System.Runtime.InteropServices.CustomQueryInterfaceResult.Handled;
                }
            }

            return global::System.Runtime.InteropServices.CustomQueryInterfaceResult.NotHandled;
        }
    }
    [global::WinRT.WindowsRuntimeType]
    [Guid("47396C1E-51B9-4207-9146-248E636A1D1D")]
    internal interface IAlternateWordForm
    {
        string AlternateText { get; }
        AlternateNormalizationFormat NormalizationFormat { get; }
        TextSegment SourceTextSegment { get; }
    }
    [global::WinRT.WindowsRuntimeType]
    [Guid("97909E87-9291-4F91-B6C8-B6E359D7A7FB")]
    internal interface IUnicodeCharactersStatics
    {
        uint GetCodepointFromSurrogatePair(uint highSurrogate, uint lowSurrogate);
        void GetSurrogatePairFromCodepoint(uint codepoint, out char highSurrogate, out char lowSurrogate);
        bool IsHighSurrogate(uint codepoint);
        bool IsLowSurrogate(uint codepoint);
        bool IsSupplementary(uint codepoint);
        bool IsNoncharacter(uint codepoint);
        bool IsWhitespace(uint codepoint);
        bool IsAlphabetic(uint codepoint);
        bool IsCased(uint codepoint);
        bool IsUppercase(uint codepoint);
        bool IsLowercase(uint codepoint);
        bool IsIdStart(uint codepoint);
        bool IsIdContinue(uint codepoint);
        bool IsGraphemeBase(uint codepoint);
        bool IsGraphemeExtend(uint codepoint);
        UnicodeNumericType GetNumericType(uint codepoint);
        UnicodeGeneralCategory GetGeneralCategory(uint codepoint);
    }
    [global::WinRT.WindowsRuntimeType]
    [Guid("D2D4BA6D-987C-4CC0-B6BD-D49A11B38F9A")]
    internal interface IWordSegment
    {
        global::System.Collections.Generic.IReadOnlyList<AlternateWordForm> AlternateForms { get; }
        TextSegment SourceTextSegment { get; }
        string Text { get; }
    }
    [global::WinRT.WindowsRuntimeType]
    [Guid("86B4D4D1-B2FE-4E34-A81D-66640300454F")]
    internal interface IWordsSegmenter
    {
        WordSegment GetTokenAt(string text, uint startIndex);
        global::System.Collections.Generic.IReadOnlyList<WordSegment> GetTokens(string text);
        void Tokenize(string text, uint startIndex, WordSegmentsTokenizingHandler handler);
        string ResolvedLanguage { get; }
    }
    [global::WinRT.WindowsRuntimeType]
    [Guid("E6977274-FC35-455C-8BFB-6D7F4653CA97")]
    internal interface IWordsSegmenterFactory
    {
        WordsSegmenter CreateWithLanguage(string language);
    }
    [global::WinRT.WindowsRuntimeType]
    internal struct TextSegment: IEquatable<TextSegment>
    {
        public uint StartPosition;
        public uint Length;

        public TextSegment(uint _StartPosition, uint _Length)
        {
            StartPosition = _StartPosition; Length = _Length; 
        }

        public static bool operator ==(TextSegment x, TextSegment y) => x.StartPosition == y.StartPosition && x.Length == y.Length;
        public static bool operator !=(TextSegment x, TextSegment y) => !(x == y);
        public bool Equals(TextSegment other) => this == other;
        public override bool Equals(object obj) => obj is TextSegment that && this == that;
        public override int GetHashCode() => StartPosition.GetHashCode() ^ Length.GetHashCode();
    }
    internal static class UnicodeCharacters
    {
        internal class _IUnicodeCharactersStatics : ABI.Windows.Data.Text.IUnicodeCharactersStatics
        {
            public _IUnicodeCharactersStatics() : base((new BaseActivationFactory("Windows.Data.Text", "Windows.Data.Text.UnicodeCharacters"))._As<ABI.Windows.Data.Text.IUnicodeCharactersStatics.Vftbl>()) { }
            private static WeakLazy<_IUnicodeCharactersStatics> _instance = new WeakLazy<_IUnicodeCharactersStatics>();
            public static IUnicodeCharactersStatics Instance => _instance.Value;
        }

        public static uint GetCodepointFromSurrogatePair(uint highSurrogate, uint lowSurrogate) => _IUnicodeCharactersStatics.Instance.GetCodepointFromSurrogatePair(highSurrogate, lowSurrogate);

        public static void GetSurrogatePairFromCodepoint(uint codepoint, out char highSurrogate, out char lowSurrogate) => _IUnicodeCharactersStatics.Instance.GetSurrogatePairFromCodepoint(codepoint, out highSurrogate, out lowSurrogate);

        public static bool IsHighSurrogate(uint codepoint) => _IUnicodeCharactersStatics.Instance.IsHighSurrogate(codepoint);

        public static bool IsLowSurrogate(uint codepoint) => _IUnicodeCharactersStatics.Instance.IsLowSurrogate(codepoint);

        public static bool IsSupplementary(uint codepoint) => _IUnicodeCharactersStatics.Instance.IsSupplementary(codepoint);

        public static bool IsNoncharacter(uint codepoint) => _IUnicodeCharactersStatics.Instance.IsNoncharacter(codepoint);

        public static bool IsWhitespace(uint codepoint) => _IUnicodeCharactersStatics.Instance.IsWhitespace(codepoint);

        public static bool IsAlphabetic(uint codepoint) => _IUnicodeCharactersStatics.Instance.IsAlphabetic(codepoint);

        public static bool IsCased(uint codepoint) => _IUnicodeCharactersStatics.Instance.IsCased(codepoint);

        public static bool IsUppercase(uint codepoint) => _IUnicodeCharactersStatics.Instance.IsUppercase(codepoint);

        public static bool IsLowercase(uint codepoint) => _IUnicodeCharactersStatics.Instance.IsLowercase(codepoint);

        public static bool IsIdStart(uint codepoint) => _IUnicodeCharactersStatics.Instance.IsIdStart(codepoint);

        public static bool IsIdContinue(uint codepoint) => _IUnicodeCharactersStatics.Instance.IsIdContinue(codepoint);

        public static bool IsGraphemeBase(uint codepoint) => _IUnicodeCharactersStatics.Instance.IsGraphemeBase(codepoint);

        public static bool IsGraphemeExtend(uint codepoint) => _IUnicodeCharactersStatics.Instance.IsGraphemeExtend(codepoint);

        public static UnicodeNumericType GetNumericType(uint codepoint) => _IUnicodeCharactersStatics.Instance.GetNumericType(codepoint);

        public static UnicodeGeneralCategory GetGeneralCategory(uint codepoint) => _IUnicodeCharactersStatics.Instance.GetGeneralCategory(codepoint);
    }[global::WinRT.WindowsRuntimeType]
    internal enum UnicodeGeneralCategory : int
    {
        UppercaseLetter = unchecked((int)0),
        LowercaseLetter = unchecked((int)0x1),
        TitlecaseLetter = unchecked((int)0x2),
        ModifierLetter = unchecked((int)0x3),
        OtherLetter = unchecked((int)0x4),
        NonspacingMark = unchecked((int)0x5),
        SpacingCombiningMark = unchecked((int)0x6),
        EnclosingMark = unchecked((int)0x7),
        DecimalDigitNumber = unchecked((int)0x8),
        LetterNumber = unchecked((int)0x9),
        OtherNumber = unchecked((int)0xa),
        SpaceSeparator = unchecked((int)0xb),
        LineSeparator = unchecked((int)0xc),
        ParagraphSeparator = unchecked((int)0xd),
        Control = unchecked((int)0xe),
        Format = unchecked((int)0xf),
        Surrogate = unchecked((int)0x10),
        PrivateUse = unchecked((int)0x11),
        ConnectorPunctuation = unchecked((int)0x12),
        DashPunctuation = unchecked((int)0x13),
        OpenPunctuation = unchecked((int)0x14),
        ClosePunctuation = unchecked((int)0x15),
        InitialQuotePunctuation = unchecked((int)0x16),
        FinalQuotePunctuation = unchecked((int)0x17),
        OtherPunctuation = unchecked((int)0x18),
        MathSymbol = unchecked((int)0x19),
        CurrencySymbol = unchecked((int)0x1a),
        ModifierSymbol = unchecked((int)0x1b),
        OtherSymbol = unchecked((int)0x1c),
        NotAssigned = unchecked((int)0x1d),
    }
    [global::WinRT.WindowsRuntimeType]
    internal enum UnicodeNumericType : int
    {
        None = unchecked((int)0),
        Decimal = unchecked((int)0x1),
        Digit = unchecked((int)0x2),
        Numeric = unchecked((int)0x3),
    }
    [global::WinRT.WindowsRuntimeType]
    [global::WinRT.ProjectedRuntimeClass(nameof(_default))]
    internal sealed class WordSegment : global::System.Runtime.InteropServices.ICustomQueryInterface, IEquatable<WordSegment>
    {
        public IntPtr ThisPtr => _default.ThisPtr;

        private IObjectReference _inner = null;
        private readonly Lazy<global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.IWordSegment> _defaultLazy;

        private global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.IWordSegment _default => _defaultLazy.Value;

        public static WordSegment FromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero) return null;
            var obj = MarshalInspectable.FromAbi(thisPtr);
            return obj is WordSegment ? (WordSegment)obj : new WordSegment((global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.IWordSegment)obj);
        }

        public WordSegment(global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.IWordSegment ifc)
        {
            _defaultLazy = new Lazy<global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.IWordSegment>(() => ifc);
        }

        public static bool operator ==(WordSegment x, WordSegment y) => (x?.ThisPtr ?? IntPtr.Zero) == (y?.ThisPtr ?? IntPtr.Zero);
        public static bool operator !=(WordSegment x, WordSegment y) => !(x == y);
        public bool Equals(WordSegment other) => this == other;
        public override bool Equals(object obj) => obj is WordSegment that && this == that;
        public override int GetHashCode() => ThisPtr.GetHashCode();

        private  IObjectReference GetDefaultReference<T>() => _default.AsInterface<T>();
        private  IObjectReference GetReferenceForQI() => _inner ?? _default.ObjRef;

        private struct InterfaceTag<I>{};

        private IWordSegment AsInternal(InterfaceTag<IWordSegment> _) => _default;

        public global::System.Collections.Generic.IReadOnlyList<AlternateWordForm> AlternateForms => _default.AlternateForms;

        public TextSegment SourceTextSegment => _default.SourceTextSegment;

        public string Text => _default.Text;

        private bool IsOverridableInterface(Guid iid) => false;

        global::System.Runtime.InteropServices.CustomQueryInterfaceResult global::System.Runtime.InteropServices.ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
        {
            ppv = IntPtr.Zero;
            if (IsOverridableInterface(iid) || typeof(global::WinRT.IInspectable).GUID == iid)
            {
                return global::System.Runtime.InteropServices.CustomQueryInterfaceResult.NotHandled;
            }

            if (GetReferenceForQI().TryAs<IUnknownVftbl>(iid, out ObjectReference<IUnknownVftbl> objRef) >= 0)
            {
                using (objRef)
                {
                    ppv = objRef.GetRef();
                    return global::System.Runtime.InteropServices.CustomQueryInterfaceResult.Handled;
                }
            }

            return global::System.Runtime.InteropServices.CustomQueryInterfaceResult.NotHandled;
        }
    }
    [global::WinRT.WindowsRuntimeType]
    internal delegate void WordSegmentsTokenizingHandler(global::System.Collections.Generic.IEnumerable<WordSegment> precedingWords, global::System.Collections.Generic.IEnumerable<WordSegment> words);
    [global::WinRT.WindowsRuntimeType]
    [global::WinRT.ProjectedRuntimeClass(nameof(_default))]
    internal sealed partial class WordsSegmenter : global::System.Runtime.InteropServices.ICustomQueryInterface, IEquatable<WordsSegmenter>
    {
        public IntPtr ThisPtr => _default.ThisPtr;

        private IObjectReference _inner = null;
        private readonly Lazy<global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.IWordsSegmenter> _defaultLazy;

        private global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.IWordsSegmenter _default => _defaultLazy.Value;

        internal class _IWordsSegmenterFactory : ABI.Windows.Data.Text.IWordsSegmenterFactory
        {
            public _IWordsSegmenterFactory() : base(ActivationFactory<WordsSegmenter>.As<ABI.Windows.Data.Text.IWordsSegmenterFactory.Vftbl>()) { }
            private static WeakLazy<_IWordsSegmenterFactory> _instance = new WeakLazy<_IWordsSegmenterFactory>();
            public static _IWordsSegmenterFactory Instance => _instance.Value;

            public unsafe new IntPtr CreateWithLanguage(string language)
            {
                MarshalString __language = default;
                IntPtr __retval = default;
                try
                {
                    __language = MarshalString.CreateMarshaler(language);
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.CreateWithLanguage_0(ThisPtr, MarshalString.GetAbi(__language), out __retval));
                    return __retval;
                }
                finally
                {
                    MarshalString.DisposeMarshaler(__language);
                }
            }

        }

        public WordsSegmenter(string language) : this(((Func<global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.IWordsSegmenter>)(() => {
            IntPtr ptr = (_IWordsSegmenterFactory.Instance.CreateWithLanguage(language));
            try
            {
                return new global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.IWordsSegmenter(ComWrappersSupport.GetObjectReferenceForInterface(ptr));
            }
            finally
            {
                MarshalInspectable.DisposeAbi(ptr);
            }
        }))())
        {
            ComWrappersSupport.RegisterObjectForInterface(this, ThisPtr);
        }

        public static WordsSegmenter FromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero) return null;
            var obj = MarshalInspectable.FromAbi(thisPtr);
            return obj is WordsSegmenter ? (WordsSegmenter)obj : new WordsSegmenter((global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.IWordsSegmenter)obj);
        }

        public WordsSegmenter(global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.IWordsSegmenter ifc)
        {
            _defaultLazy = new Lazy<global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.IWordsSegmenter>(() => ifc);
        }

        public static bool operator ==(WordsSegmenter x, WordsSegmenter y) => (x?.ThisPtr ?? IntPtr.Zero) == (y?.ThisPtr ?? IntPtr.Zero);
        public static bool operator !=(WordsSegmenter x, WordsSegmenter y) => !(x == y);
        public bool Equals(WordsSegmenter other) => this == other;
        public override bool Equals(object obj) => obj is WordsSegmenter that && this == that;
        public override int GetHashCode() => ThisPtr.GetHashCode();

        private  IObjectReference GetDefaultReference<T>() => _default.AsInterface<T>();
        private  IObjectReference GetReferenceForQI() => _inner ?? _default.ObjRef;

        private struct InterfaceTag<I>{};

        private IWordsSegmenter AsInternal(InterfaceTag<IWordsSegmenter> _) => _default;

        public WordSegment GetTokenAt(string text, uint startIndex) => _default.GetTokenAt(text, startIndex);

        public global::System.Collections.Generic.IReadOnlyList<WordSegment> GetTokens(string text) => _default.GetTokens(text);

        public void Tokenize(string text, uint startIndex, WordSegmentsTokenizingHandler handler) => _default.Tokenize(text, startIndex, handler);

        public string ResolvedLanguage => _default.ResolvedLanguage;

        private bool IsOverridableInterface(Guid iid) => false;

        global::System.Runtime.InteropServices.CustomQueryInterfaceResult global::System.Runtime.InteropServices.ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
        {
            ppv = IntPtr.Zero;
            if (IsOverridableInterface(iid) || typeof(global::WinRT.IInspectable).GUID == iid)
            {
                return global::System.Runtime.InteropServices.CustomQueryInterfaceResult.NotHandled;
            }

            if (GetReferenceForQI().TryAs<IUnknownVftbl>(iid, out ObjectReference<IUnknownVftbl> objRef) >= 0)
            {
                using (objRef)
                {
                    ppv = objRef.GetRef();
                    return global::System.Runtime.InteropServices.CustomQueryInterfaceResult.Handled;
                }
            }

            return global::System.Runtime.InteropServices.CustomQueryInterfaceResult.NotHandled;
        }
    }
}

namespace ABI.Windows.Data.Text
{
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal struct AlternateWordForm
    {
        public static IObjectReference CreateMarshaler(global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateWordForm obj) => obj is null ? null : MarshalInspectable.CreateMarshaler(obj).As<IAlternateWordForm.Vftbl>();
        public static IntPtr GetAbi(IObjectReference value) => value is null ? IntPtr.Zero : MarshalInterfaceHelper<object>.GetAbi(value);
        public static global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateWordForm FromAbi(IntPtr thisPtr) => global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateWordForm.FromAbi(thisPtr);
        public static IntPtr FromManaged(global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateWordForm obj) => obj is null ? IntPtr.Zero : CreateMarshaler(obj).GetRef();
        public static unsafe MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateWordForm>.MarshalerArray CreateMarshalerArray(global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateWordForm[] array) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateWordForm>.CreateMarshalerArray(array, (o) => CreateMarshaler(o));
        public static (int length, IntPtr data) GetAbiArray(object box) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateWordForm>.GetAbiArray(box);
        public static unsafe global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateWordForm[] FromAbiArray(object box) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateWordForm>.FromAbiArray(box, FromAbi);
        public static (int length, IntPtr data) FromManagedArray(global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateWordForm[] array) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateWordForm>.FromManagedArray(array, (o) => FromManaged(o));
        public static void DisposeMarshaler(IObjectReference value) => MarshalInspectable.DisposeMarshaler(value);
        public static void DisposeMarshalerArray(MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateWordForm>.MarshalerArray array) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateWordForm>.DisposeMarshalerArray(array);
        public static void DisposeAbi(IntPtr abi) => MarshalInspectable.DisposeAbi(abi);
        public static unsafe void DisposeAbiArray(object box) => MarshalInspectable.DisposeAbiArray(box);
    }
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("47396C1E-51B9-4207-9146-248E636A1D1D")]
    internal class IAlternateWordForm : global::MS.Internal.WindowsRuntime.Windows.Data.Text.IAlternateWordForm
    {
        [Guid("47396C1E-51B9-4207-9146-248E636A1D1D")]
        internal struct Vftbl
        {
            public IInspectable.Vftbl IInspectableVftbl;
            public IAlternateWordForm_Delegates.get_SourceTextSegment_0 get_SourceTextSegment_0;
            public _get_PropertyAsString get_AlternateText_1;
            public IAlternateWordForm_Delegates.get_NormalizationFormat_2 get_NormalizationFormat_2;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable, 
                    get_SourceTextSegment_0 = Do_Abi_get_SourceTextSegment_0,
                    get_AlternateText_1 = Do_Abi_get_AlternateText_1,
                    get_NormalizationFormat_2 = Do_Abi_get_NormalizationFormat_2
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 3);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_get_AlternateText_1(IntPtr thisPtr, out IntPtr value)
            {
                string __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IAlternateWordForm>(thisPtr).AlternateText;
                    value = MarshalString.FromManaged(__value);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_NormalizationFormat_2(IntPtr thisPtr, out global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateNormalizationFormat value)
            {
                global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateNormalizationFormat __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IAlternateWordForm>(thisPtr).NormalizationFormat;
                    value = __value;

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_SourceTextSegment_0(IntPtr thisPtr, out global::MS.Internal.WindowsRuntime.Windows.Data.Text.TextSegment value)
            {
                global::MS.Internal.WindowsRuntime.Windows.Data.Text.TextSegment __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IAlternateWordForm>(thisPtr).SourceTextSegment;
                    value = __value;

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IAlternateWordForm(IObjectReference obj) => (obj != null) ? new IAlternateWordForm(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IAlternateWordForm(IObjectReference obj) : this(obj.As<Vftbl>()) {}
        public IAlternateWordForm(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe string AlternateText
        {
            get
            {
                IntPtr __retval = default;
                try
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_AlternateText_1(ThisPtr, out __retval));
                    return MarshalString.FromAbi(__retval);
                }
                finally
                {
                    MarshalString.DisposeAbi(__retval);
                }
            }
        }

        public unsafe global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateNormalizationFormat NormalizationFormat
        {
            get
            {
                global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateNormalizationFormat __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_NormalizationFormat_2(ThisPtr, out __retval));
                return __retval;
            }
        }

        public unsafe global::MS.Internal.WindowsRuntime.Windows.Data.Text.TextSegment SourceTextSegment
        {
            get
            {
                global::MS.Internal.WindowsRuntime.Windows.Data.Text.TextSegment __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_SourceTextSegment_0(ThisPtr, out __retval));
                return __retval;
            }
        }
    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static class IAlternateWordForm_Delegates
    {
        public unsafe delegate int get_SourceTextSegment_0(IntPtr thisPtr, out global::MS.Internal.WindowsRuntime.Windows.Data.Text.TextSegment value);
        public unsafe delegate int get_NormalizationFormat_2(IntPtr thisPtr, out global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateNormalizationFormat value);
    }

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("97909E87-9291-4F91-B6C8-B6E359D7A7FB")]
    internal class IUnicodeCharactersStatics : global::MS.Internal.WindowsRuntime.Windows.Data.Text.IUnicodeCharactersStatics
    {
        [Guid("97909E87-9291-4F91-B6C8-B6E359D7A7FB")]
        internal struct Vftbl
        {
            public IInspectable.Vftbl IInspectableVftbl;
            public IUnicodeCharactersStatics_Delegates.GetCodepointFromSurrogatePair_0 GetCodepointFromSurrogatePair_0;
            public IUnicodeCharactersStatics_Delegates.GetSurrogatePairFromCodepoint_1 GetSurrogatePairFromCodepoint_1;
            public IUnicodeCharactersStatics_Delegates.IsHighSurrogate_2 IsHighSurrogate_2;
            public IUnicodeCharactersStatics_Delegates.IsLowSurrogate_3 IsLowSurrogate_3;
            public IUnicodeCharactersStatics_Delegates.IsSupplementary_4 IsSupplementary_4;
            public IUnicodeCharactersStatics_Delegates.IsNoncharacter_5 IsNoncharacter_5;
            public IUnicodeCharactersStatics_Delegates.IsWhitespace_6 IsWhitespace_6;
            public IUnicodeCharactersStatics_Delegates.IsAlphabetic_7 IsAlphabetic_7;
            public IUnicodeCharactersStatics_Delegates.IsCased_8 IsCased_8;
            public IUnicodeCharactersStatics_Delegates.IsUppercase_9 IsUppercase_9;
            public IUnicodeCharactersStatics_Delegates.IsLowercase_10 IsLowercase_10;
            public IUnicodeCharactersStatics_Delegates.IsIdStart_11 IsIdStart_11;
            public IUnicodeCharactersStatics_Delegates.IsIdContinue_12 IsIdContinue_12;
            public IUnicodeCharactersStatics_Delegates.IsGraphemeBase_13 IsGraphemeBase_13;
            public IUnicodeCharactersStatics_Delegates.IsGraphemeExtend_14 IsGraphemeExtend_14;
            public IUnicodeCharactersStatics_Delegates.GetNumericType_15 GetNumericType_15;
            public IUnicodeCharactersStatics_Delegates.GetGeneralCategory_16 GetGeneralCategory_16;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable, 
                    GetCodepointFromSurrogatePair_0 = Do_Abi_GetCodepointFromSurrogatePair_0,
                    GetSurrogatePairFromCodepoint_1 = Do_Abi_GetSurrogatePairFromCodepoint_1,
                    IsHighSurrogate_2 = Do_Abi_IsHighSurrogate_2,
                    IsLowSurrogate_3 = Do_Abi_IsLowSurrogate_3,
                    IsSupplementary_4 = Do_Abi_IsSupplementary_4,
                    IsNoncharacter_5 = Do_Abi_IsNoncharacter_5,
                    IsWhitespace_6 = Do_Abi_IsWhitespace_6,
                    IsAlphabetic_7 = Do_Abi_IsAlphabetic_7,
                    IsCased_8 = Do_Abi_IsCased_8,
                    IsUppercase_9 = Do_Abi_IsUppercase_9,
                    IsLowercase_10 = Do_Abi_IsLowercase_10,
                    IsIdStart_11 = Do_Abi_IsIdStart_11,
                    IsIdContinue_12 = Do_Abi_IsIdContinue_12,
                    IsGraphemeBase_13 = Do_Abi_IsGraphemeBase_13,
                    IsGraphemeExtend_14 = Do_Abi_IsGraphemeExtend_14,
                    GetNumericType_15 = Do_Abi_GetNumericType_15,
                    GetGeneralCategory_16 = Do_Abi_GetGeneralCategory_16
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 17);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_GetCodepointFromSurrogatePair_0(IntPtr thisPtr, uint highSurrogate, uint lowSurrogate, out uint codepoint)
            {
                uint __codepoint = default;

                codepoint = default;

                try
                {
                    __codepoint = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IUnicodeCharactersStatics>(thisPtr).GetCodepointFromSurrogatePair(highSurrogate, lowSurrogate);
                    codepoint = __codepoint;

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_GetSurrogatePairFromCodepoint_1(IntPtr thisPtr, uint codepoint, out ushort highSurrogate, out ushort lowSurrogate)
            {

                highSurrogate = default;
                lowSurrogate = default;
                char __highSurrogate = default;
                char __lowSurrogate = default;

                try
                {
                    global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IUnicodeCharactersStatics>(thisPtr).GetSurrogatePairFromCodepoint(codepoint, out __highSurrogate, out __lowSurrogate);
                    highSurrogate = (ushort)__highSurrogate;
                    lowSurrogate = (ushort)__lowSurrogate;

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_IsHighSurrogate_2(IntPtr thisPtr, uint codepoint, out byte value)
            {
                bool __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IUnicodeCharactersStatics>(thisPtr).IsHighSurrogate(codepoint);
                    value = (byte)(__value ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_IsLowSurrogate_3(IntPtr thisPtr, uint codepoint, out byte value)
            {
                bool __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IUnicodeCharactersStatics>(thisPtr).IsLowSurrogate(codepoint);
                    value = (byte)(__value ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_IsSupplementary_4(IntPtr thisPtr, uint codepoint, out byte value)
            {
                bool __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IUnicodeCharactersStatics>(thisPtr).IsSupplementary(codepoint);
                    value = (byte)(__value ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_IsNoncharacter_5(IntPtr thisPtr, uint codepoint, out byte value)
            {
                bool __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IUnicodeCharactersStatics>(thisPtr).IsNoncharacter(codepoint);
                    value = (byte)(__value ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_IsWhitespace_6(IntPtr thisPtr, uint codepoint, out byte value)
            {
                bool __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IUnicodeCharactersStatics>(thisPtr).IsWhitespace(codepoint);
                    value = (byte)(__value ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_IsAlphabetic_7(IntPtr thisPtr, uint codepoint, out byte value)
            {
                bool __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IUnicodeCharactersStatics>(thisPtr).IsAlphabetic(codepoint);
                    value = (byte)(__value ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_IsCased_8(IntPtr thisPtr, uint codepoint, out byte value)
            {
                bool __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IUnicodeCharactersStatics>(thisPtr).IsCased(codepoint);
                    value = (byte)(__value ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_IsUppercase_9(IntPtr thisPtr, uint codepoint, out byte value)
            {
                bool __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IUnicodeCharactersStatics>(thisPtr).IsUppercase(codepoint);
                    value = (byte)(__value ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_IsLowercase_10(IntPtr thisPtr, uint codepoint, out byte value)
            {
                bool __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IUnicodeCharactersStatics>(thisPtr).IsLowercase(codepoint);
                    value = (byte)(__value ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_IsIdStart_11(IntPtr thisPtr, uint codepoint, out byte value)
            {
                bool __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IUnicodeCharactersStatics>(thisPtr).IsIdStart(codepoint);
                    value = (byte)(__value ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_IsIdContinue_12(IntPtr thisPtr, uint codepoint, out byte value)
            {
                bool __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IUnicodeCharactersStatics>(thisPtr).IsIdContinue(codepoint);
                    value = (byte)(__value ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_IsGraphemeBase_13(IntPtr thisPtr, uint codepoint, out byte value)
            {
                bool __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IUnicodeCharactersStatics>(thisPtr).IsGraphemeBase(codepoint);
                    value = (byte)(__value ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_IsGraphemeExtend_14(IntPtr thisPtr, uint codepoint, out byte value)
            {
                bool __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IUnicodeCharactersStatics>(thisPtr).IsGraphemeExtend(codepoint);
                    value = (byte)(__value ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_GetNumericType_15(IntPtr thisPtr, uint codepoint, out global::MS.Internal.WindowsRuntime.Windows.Data.Text.UnicodeNumericType value)
            {
                global::MS.Internal.WindowsRuntime.Windows.Data.Text.UnicodeNumericType __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IUnicodeCharactersStatics>(thisPtr).GetNumericType(codepoint);
                    value = __value;

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_GetGeneralCategory_16(IntPtr thisPtr, uint codepoint, out global::MS.Internal.WindowsRuntime.Windows.Data.Text.UnicodeGeneralCategory value)
            {
                global::MS.Internal.WindowsRuntime.Windows.Data.Text.UnicodeGeneralCategory __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IUnicodeCharactersStatics>(thisPtr).GetGeneralCategory(codepoint);
                    value = __value;

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IUnicodeCharactersStatics(IObjectReference obj) => (obj != null) ? new IUnicodeCharactersStatics(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IUnicodeCharactersStatics(IObjectReference obj) : this(obj.As<Vftbl>()) {}
        public IUnicodeCharactersStatics(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe uint GetCodepointFromSurrogatePair(uint highSurrogate, uint lowSurrogate)
        {
            uint __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetCodepointFromSurrogatePair_0(ThisPtr, highSurrogate, lowSurrogate, out __retval));
            return __retval;
        }

        public unsafe void GetSurrogatePairFromCodepoint(uint codepoint, out char highSurrogate, out char lowSurrogate)
        {
            ushort __highSurrogate = default;
            ushort __lowSurrogate = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetSurrogatePairFromCodepoint_1(ThisPtr, codepoint, out __highSurrogate, out __lowSurrogate));
            highSurrogate = (char)__highSurrogate;
            lowSurrogate = (char)__lowSurrogate;
        }

        public unsafe bool IsHighSurrogate(uint codepoint)
        {
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.IsHighSurrogate_2(ThisPtr, codepoint, out __retval));
            return __retval != 0;
        }

        public unsafe bool IsLowSurrogate(uint codepoint)
        {
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.IsLowSurrogate_3(ThisPtr, codepoint, out __retval));
            return __retval != 0;
        }

        public unsafe bool IsSupplementary(uint codepoint)
        {
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.IsSupplementary_4(ThisPtr, codepoint, out __retval));
            return __retval != 0;
        }

        public unsafe bool IsNoncharacter(uint codepoint)
        {
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.IsNoncharacter_5(ThisPtr, codepoint, out __retval));
            return __retval != 0;
        }

        public unsafe bool IsWhitespace(uint codepoint)
        {
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.IsWhitespace_6(ThisPtr, codepoint, out __retval));
            return __retval != 0;
        }

        public unsafe bool IsAlphabetic(uint codepoint)
        {
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.IsAlphabetic_7(ThisPtr, codepoint, out __retval));
            return __retval != 0;
        }

        public unsafe bool IsCased(uint codepoint)
        {
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.IsCased_8(ThisPtr, codepoint, out __retval));
            return __retval != 0;
        }

        public unsafe bool IsUppercase(uint codepoint)
        {
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.IsUppercase_9(ThisPtr, codepoint, out __retval));
            return __retval != 0;
        }

        public unsafe bool IsLowercase(uint codepoint)
        {
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.IsLowercase_10(ThisPtr, codepoint, out __retval));
            return __retval != 0;
        }

        public unsafe bool IsIdStart(uint codepoint)
        {
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.IsIdStart_11(ThisPtr, codepoint, out __retval));
            return __retval != 0;
        }

        public unsafe bool IsIdContinue(uint codepoint)
        {
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.IsIdContinue_12(ThisPtr, codepoint, out __retval));
            return __retval != 0;
        }

        public unsafe bool IsGraphemeBase(uint codepoint)
        {
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.IsGraphemeBase_13(ThisPtr, codepoint, out __retval));
            return __retval != 0;
        }

        public unsafe bool IsGraphemeExtend(uint codepoint)
        {
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.IsGraphemeExtend_14(ThisPtr, codepoint, out __retval));
            return __retval != 0;
        }

        public unsafe global::MS.Internal.WindowsRuntime.Windows.Data.Text.UnicodeNumericType GetNumericType(uint codepoint)
        {
            global::MS.Internal.WindowsRuntime.Windows.Data.Text.UnicodeNumericType __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetNumericType_15(ThisPtr, codepoint, out __retval));
            return __retval;
        }

        public unsafe global::MS.Internal.WindowsRuntime.Windows.Data.Text.UnicodeGeneralCategory GetGeneralCategory(uint codepoint)
        {
            global::MS.Internal.WindowsRuntime.Windows.Data.Text.UnicodeGeneralCategory __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetGeneralCategory_16(ThisPtr, codepoint, out __retval));
            return __retval;
        }
    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static class IUnicodeCharactersStatics_Delegates
    {
        public unsafe delegate int GetCodepointFromSurrogatePair_0(IntPtr thisPtr, uint highSurrogate, uint lowSurrogate, out uint codepoint);
        public unsafe delegate int GetSurrogatePairFromCodepoint_1(IntPtr thisPtr, uint codepoint, out ushort highSurrogate, out ushort lowSurrogate);
        public unsafe delegate int IsHighSurrogate_2(IntPtr thisPtr, uint codepoint, out byte value);
        public unsafe delegate int IsLowSurrogate_3(IntPtr thisPtr, uint codepoint, out byte value);
        public unsafe delegate int IsSupplementary_4(IntPtr thisPtr, uint codepoint, out byte value);
        public unsafe delegate int IsNoncharacter_5(IntPtr thisPtr, uint codepoint, out byte value);
        public unsafe delegate int IsWhitespace_6(IntPtr thisPtr, uint codepoint, out byte value);
        public unsafe delegate int IsAlphabetic_7(IntPtr thisPtr, uint codepoint, out byte value);
        public unsafe delegate int IsCased_8(IntPtr thisPtr, uint codepoint, out byte value);
        public unsafe delegate int IsUppercase_9(IntPtr thisPtr, uint codepoint, out byte value);
        public unsafe delegate int IsLowercase_10(IntPtr thisPtr, uint codepoint, out byte value);
        public unsafe delegate int IsIdStart_11(IntPtr thisPtr, uint codepoint, out byte value);
        public unsafe delegate int IsIdContinue_12(IntPtr thisPtr, uint codepoint, out byte value);
        public unsafe delegate int IsGraphemeBase_13(IntPtr thisPtr, uint codepoint, out byte value);
        public unsafe delegate int IsGraphemeExtend_14(IntPtr thisPtr, uint codepoint, out byte value);
        public unsafe delegate int GetNumericType_15(IntPtr thisPtr, uint codepoint, out global::MS.Internal.WindowsRuntime.Windows.Data.Text.UnicodeNumericType value);
        public unsafe delegate int GetGeneralCategory_16(IntPtr thisPtr, uint codepoint, out global::MS.Internal.WindowsRuntime.Windows.Data.Text.UnicodeGeneralCategory value);
    }

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("D2D4BA6D-987C-4CC0-B6BD-D49A11B38F9A")]
    internal class IWordSegment : global::MS.Internal.WindowsRuntime.Windows.Data.Text.IWordSegment
    {
        [Guid("D2D4BA6D-987C-4CC0-B6BD-D49A11B38F9A")]
        internal struct Vftbl
        {
            public IInspectable.Vftbl IInspectableVftbl;
            public _get_PropertyAsString get_Text_0;
            public IWordSegment_Delegates.get_SourceTextSegment_1 get_SourceTextSegment_1;
            public _get_PropertyAsObject get_AlternateForms_2;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable, 
                    get_Text_0 = Do_Abi_get_Text_0,
                    get_SourceTextSegment_1 = Do_Abi_get_SourceTextSegment_1,
                    get_AlternateForms_2 = Do_Abi_get_AlternateForms_2
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 3);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_get_AlternateForms_2(IntPtr thisPtr, out IntPtr value)
            {
                global::System.Collections.Generic.IReadOnlyList<global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateWordForm> __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IWordSegment>(thisPtr).AlternateForms;
                    value = global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IReadOnlyList<global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateWordForm>.FromManaged(__value);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_SourceTextSegment_1(IntPtr thisPtr, out global::MS.Internal.WindowsRuntime.Windows.Data.Text.TextSegment value)
            {
                global::MS.Internal.WindowsRuntime.Windows.Data.Text.TextSegment __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IWordSegment>(thisPtr).SourceTextSegment;
                    value = __value;

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_Text_0(IntPtr thisPtr, out IntPtr value)
            {
                string __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IWordSegment>(thisPtr).Text;
                    value = MarshalString.FromManaged(__value);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IWordSegment(IObjectReference obj) => (obj != null) ? new IWordSegment(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IWordSegment(IObjectReference obj) : this(obj.As<Vftbl>()) {}
        public IWordSegment(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe global::System.Collections.Generic.IReadOnlyList<global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateWordForm> AlternateForms
        {
            get
            {
                IntPtr __retval = default;
                try
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_AlternateForms_2(ThisPtr, out __retval));
                    return global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IReadOnlyList<global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateWordForm>.FromAbi(__retval);
                }
                finally
                {
                    global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IReadOnlyList<global::MS.Internal.WindowsRuntime.Windows.Data.Text.AlternateWordForm>.DisposeAbi(__retval);
                }
            }
        }

        public unsafe global::MS.Internal.WindowsRuntime.Windows.Data.Text.TextSegment SourceTextSegment
        {
            get
            {
                global::MS.Internal.WindowsRuntime.Windows.Data.Text.TextSegment __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_SourceTextSegment_1(ThisPtr, out __retval));
                return __retval;
            }
        }

        public unsafe string Text
        {
            get
            {
                IntPtr __retval = default;
                try
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_Text_0(ThisPtr, out __retval));
                    return MarshalString.FromAbi(__retval);
                }
                finally
                {
                    MarshalString.DisposeAbi(__retval);
                }
            }
        }
    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static class IWordSegment_Delegates
    {
        public unsafe delegate int get_SourceTextSegment_1(IntPtr thisPtr, out global::MS.Internal.WindowsRuntime.Windows.Data.Text.TextSegment value);
    }

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("86B4D4D1-B2FE-4E34-A81D-66640300454F")]
    internal class IWordsSegmenter : global::MS.Internal.WindowsRuntime.Windows.Data.Text.IWordsSegmenter
    {
        [Guid("86B4D4D1-B2FE-4E34-A81D-66640300454F")]
        internal struct Vftbl
        {
            public IInspectable.Vftbl IInspectableVftbl;
            public _get_PropertyAsString get_ResolvedLanguage_0;
            public IWordsSegmenter_Delegates.GetTokenAt_1 GetTokenAt_1;
            public IWordsSegmenter_Delegates.GetTokens_2 GetTokens_2;
            public IWordsSegmenter_Delegates.Tokenize_3 Tokenize_3;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable, 
                    get_ResolvedLanguage_0 = Do_Abi_get_ResolvedLanguage_0,
                    GetTokenAt_1 = Do_Abi_GetTokenAt_1,
                    GetTokens_2 = Do_Abi_GetTokens_2,
                    Tokenize_3 = Do_Abi_Tokenize_3
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 4);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_GetTokenAt_1(IntPtr thisPtr, IntPtr text, uint startIndex, out IntPtr result)
            {
                global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment __result = default;

                result = default;

                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IWordsSegmenter>(thisPtr).GetTokenAt(MarshalString.FromAbi(text), startIndex);
                    result = global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.WordSegment.FromManaged(__result);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_GetTokens_2(IntPtr thisPtr, IntPtr text, out IntPtr result)
            {
                global::System.Collections.Generic.IReadOnlyList<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment> __result = default;

                result = default;

                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IWordsSegmenter>(thisPtr).GetTokens(MarshalString.FromAbi(text));
                    result = global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IReadOnlyList<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment>.FromManaged(__result);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_Tokenize_3(IntPtr thisPtr, IntPtr text, uint startIndex, IntPtr handler)
            {


                try
                {
                    global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IWordsSegmenter>(thisPtr).Tokenize(MarshalString.FromAbi(text), startIndex, global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.WordSegmentsTokenizingHandler.FromAbi(handler));

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_ResolvedLanguage_0(IntPtr thisPtr, out IntPtr value)
            {
                string __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IWordsSegmenter>(thisPtr).ResolvedLanguage;
                    value = MarshalString.FromManaged(__value);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IWordsSegmenter(IObjectReference obj) => (obj != null) ? new IWordsSegmenter(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IWordsSegmenter(IObjectReference obj) : this(obj.As<Vftbl>()) {}
        public IWordsSegmenter(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment GetTokenAt(string text, uint startIndex)
        {
            MarshalString __text = default;
            IntPtr __retval = default;
            try
            {
                __text = MarshalString.CreateMarshaler(text);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetTokenAt_1(ThisPtr, MarshalString.GetAbi(__text), startIndex, out __retval));
                return global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.WordSegment.FromAbi(__retval);
            }
            finally
            {
                MarshalString.DisposeMarshaler(__text);
                global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.WordSegment.DisposeAbi(__retval);
            }
        }

        public unsafe global::System.Collections.Generic.IReadOnlyList<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment> GetTokens(string text)
        {
            MarshalString __text = default;
            IntPtr __retval = default;
            try
            {
                __text = MarshalString.CreateMarshaler(text);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetTokens_2(ThisPtr, MarshalString.GetAbi(__text), out __retval));
                return global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IReadOnlyList<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment>.FromAbi(__retval);
            }
            finally
            {
                MarshalString.DisposeMarshaler(__text);
                global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IReadOnlyList<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment>.DisposeAbi(__retval);
            }
        }

        public unsafe void Tokenize(string text, uint startIndex, global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegmentsTokenizingHandler handler)
        {
            MarshalString __text = default;
            IObjectReference __handler = default;
            try
            {
                __text = MarshalString.CreateMarshaler(text);
                __handler = WordSegmentsTokenizingHandler.CreateMarshaler(handler);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Tokenize_3(ThisPtr, MarshalString.GetAbi(__text), startIndex, WordSegmentsTokenizingHandler.GetAbi(__handler)));
            }
            finally
            {
                MarshalString.DisposeMarshaler(__text);
                WordSegmentsTokenizingHandler.DisposeMarshaler(__handler);
            }
        }

        public unsafe string ResolvedLanguage
        {
            get
            {
                IntPtr __retval = default;
                try
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_ResolvedLanguage_0(ThisPtr, out __retval));
                    return MarshalString.FromAbi(__retval);
                }
                finally
                {
                    MarshalString.DisposeAbi(__retval);
                }
            }
        }
    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static class IWordsSegmenter_Delegates
    {
        public unsafe delegate int GetTokenAt_1(IntPtr thisPtr, IntPtr text, uint startIndex, out IntPtr result);
        public unsafe delegate int GetTokens_2(IntPtr thisPtr, IntPtr text, out IntPtr result);
        public unsafe delegate int Tokenize_3(IntPtr thisPtr, IntPtr text, uint startIndex, IntPtr handler);
    }

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("E6977274-FC35-455C-8BFB-6D7F4653CA97")]
    internal class IWordsSegmenterFactory : global::MS.Internal.WindowsRuntime.Windows.Data.Text.IWordsSegmenterFactory
    {
        [Guid("E6977274-FC35-455C-8BFB-6D7F4653CA97")]
        internal struct Vftbl
        {
            public IInspectable.Vftbl IInspectableVftbl;
            public IWordsSegmenterFactory_Delegates.CreateWithLanguage_0 CreateWithLanguage_0;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable, 
                    CreateWithLanguage_0 = Do_Abi_CreateWithLanguage_0
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_CreateWithLanguage_0(IntPtr thisPtr, IntPtr language, out IntPtr result)
            {
                global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordsSegmenter __result = default;

                result = default;

                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Data.Text.IWordsSegmenterFactory>(thisPtr).CreateWithLanguage(MarshalString.FromAbi(language));
                    result = global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.WordsSegmenter.FromManaged(__result);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IWordsSegmenterFactory(IObjectReference obj) => (obj != null) ? new IWordsSegmenterFactory(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IWordsSegmenterFactory(IObjectReference obj) : this(obj.As<Vftbl>()) {}
        public IWordsSegmenterFactory(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordsSegmenter CreateWithLanguage(string language)
        {
            MarshalString __language = default;
            IntPtr __retval = default;
            try
            {
                __language = MarshalString.CreateMarshaler(language);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.CreateWithLanguage_0(ThisPtr, MarshalString.GetAbi(__language), out __retval));
                return global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.WordsSegmenter.FromAbi(__retval);
            }
            finally
            {
                MarshalString.DisposeMarshaler(__language);
                global::MS.Internal.WindowsRuntime.ABI.Windows.Data.Text.WordsSegmenter.DisposeAbi(__retval);
            }
        }
    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static class IWordsSegmenterFactory_Delegates
    {
        public unsafe delegate int CreateWithLanguage_0(IntPtr thisPtr, IntPtr language, out IntPtr result);
    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal struct WordSegment
    {
        public static IObjectReference CreateMarshaler(global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment obj) => obj is null ? null : MarshalInspectable.CreateMarshaler(obj).As<IWordSegment.Vftbl>();
        public static IntPtr GetAbi(IObjectReference value) => value is null ? IntPtr.Zero : MarshalInterfaceHelper<object>.GetAbi(value);
        public static global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment FromAbi(IntPtr thisPtr) => global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment.FromAbi(thisPtr);
        public static IntPtr FromManaged(global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment obj) => obj is null ? IntPtr.Zero : CreateMarshaler(obj).GetRef();
        public static unsafe MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment>.MarshalerArray CreateMarshalerArray(global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment[] array) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment>.CreateMarshalerArray(array, (o) => CreateMarshaler(o));
        public static (int length, IntPtr data) GetAbiArray(object box) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment>.GetAbiArray(box);
        public static unsafe global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment[] FromAbiArray(object box) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment>.FromAbiArray(box, FromAbi);
        public static (int length, IntPtr data) FromManagedArray(global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment[] array) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment>.FromManagedArray(array, (o) => FromManaged(o));
        public static void DisposeMarshaler(IObjectReference value) => MarshalInspectable.DisposeMarshaler(value);
        public static void DisposeMarshalerArray(MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment>.MarshalerArray array) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment>.DisposeMarshalerArray(array);
        public static void DisposeAbi(IntPtr abi) => MarshalInspectable.DisposeAbi(abi);
        public static unsafe void DisposeAbiArray(object box) => MarshalInspectable.DisposeAbiArray(box);
    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    [Guid("A5DD6357-BF2A-4C4F-A31F-29E71C6F8B35")]
    internal static class WordSegmentsTokenizingHandler
    {
        private unsafe delegate int Abi_Invoke(IntPtr thisPtr, IntPtr precedingWords, IntPtr words);

        private static readonly global::WinRT.Interop.IDelegateVftbl AbiToProjectionVftable;
        public static readonly IntPtr AbiToProjectionVftablePtr;

        static WordSegmentsTokenizingHandler()
        {
            AbiInvokeDelegate = new Abi_Invoke(Do_Abi_Invoke);
            AbiToProjectionVftable = new global::WinRT.Interop.IDelegateVftbl
            {
                IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                Invoke = Marshal.GetFunctionPointerForDelegate(AbiInvokeDelegate)
            };
            var nativeVftbl = ComWrappersSupport.AllocateVtableMemory(typeof(WordSegmentsTokenizingHandler), Marshal.SizeOf<global::WinRT.Interop.IDelegateVftbl>());
            Marshal.StructureToPtr(AbiToProjectionVftable, nativeVftbl, false);
            AbiToProjectionVftablePtr = nativeVftbl;
        }

        public static global::System.Delegate AbiInvokeDelegate { get ; }

        public static unsafe IObjectReference CreateMarshaler(global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegmentsTokenizingHandler managedDelegate) => 
        managedDelegate is null ? null : ComWrappersSupport.CreateCCWForObject(managedDelegate).As<global::WinRT.Interop.IDelegateVftbl>(GuidGenerator.GetIID(typeof(WordSegmentsTokenizingHandler)));

        public static IntPtr GetAbi(IObjectReference value) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegmentsTokenizingHandler>.GetAbi(value);

        public static unsafe global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegmentsTokenizingHandler FromAbi(IntPtr nativeDelegate)
        {
            var abiDelegate = ObjectReference<IDelegateVftbl>.FromAbi(nativeDelegate);
            return abiDelegate is null ? null : (global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegmentsTokenizingHandler)ComWrappersSupport.TryRegisterObjectForInterface(new global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegmentsTokenizingHandler(new NativeDelegateWrapper(abiDelegate).Invoke), nativeDelegate);
        }

        [global::WinRT.ObjectReferenceWrapper(nameof(_nativeDelegate))]
        private class NativeDelegateWrapper
        {
            private readonly ObjectReference<global::WinRT.Interop.IDelegateVftbl> _nativeDelegate;

            public NativeDelegateWrapper(ObjectReference<global::WinRT.Interop.IDelegateVftbl> nativeDelegate)
            {
                _nativeDelegate = nativeDelegate;
            }

            public void Invoke(global::System.Collections.Generic.IEnumerable<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment> precedingWords, global::System.Collections.Generic.IEnumerable<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment> words)
            {
                IntPtr ThisPtr = _nativeDelegate.ThisPtr;
                var abiInvoke = Marshal.GetDelegateForFunctionPointer<Abi_Invoke>(_nativeDelegate.Vftbl.Invoke);
                IObjectReference __precedingWords = default;
                IObjectReference __words = default;
                try
                {
                    __precedingWords = global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IEnumerable<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment>.CreateMarshaler(precedingWords);
                    __words = global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IEnumerable<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment>.CreateMarshaler(words);
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(abiInvoke(ThisPtr, global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IEnumerable<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment>.GetAbi(__precedingWords), global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IEnumerable<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment>.GetAbi(__words)));
                }
                finally
                {
                    global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IEnumerable<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment>.DisposeMarshaler(__precedingWords);
                    global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IEnumerable<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment>.DisposeMarshaler(__words);
                }

            }
        }

        public static IntPtr FromManaged(global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegmentsTokenizingHandler managedDelegate) => CreateMarshaler(managedDelegate)?.GetRef() ?? IntPtr.Zero;

        public static void DisposeMarshaler(IObjectReference value) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegmentsTokenizingHandler>.DisposeMarshaler(value);

        public static void DisposeAbi(IntPtr abi) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegmentsTokenizingHandler>.DisposeAbi(abi);

        private static unsafe int Do_Abi_Invoke(IntPtr thisPtr, IntPtr precedingWords, IntPtr words)
        {


            try
            {
                global::WinRT.ComWrappersSupport.MarshalDelegateInvoke(thisPtr, (global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegmentsTokenizingHandler invoke) =>
                {
                    invoke(global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IEnumerable<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment>.FromAbi(precedingWords), global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IEnumerable<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordSegment>.FromAbi(words));
                });

            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
    }

    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal struct WordsSegmenter
    {
        public static IObjectReference CreateMarshaler(global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordsSegmenter obj) => obj is null ? null : MarshalInspectable.CreateMarshaler(obj).As<IWordsSegmenter.Vftbl>();
        public static IntPtr GetAbi(IObjectReference value) => value is null ? IntPtr.Zero : MarshalInterfaceHelper<object>.GetAbi(value);
        public static global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordsSegmenter FromAbi(IntPtr thisPtr) => global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordsSegmenter.FromAbi(thisPtr);
        public static IntPtr FromManaged(global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordsSegmenter obj) => obj is null ? IntPtr.Zero : CreateMarshaler(obj).GetRef();
        public static unsafe MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordsSegmenter>.MarshalerArray CreateMarshalerArray(global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordsSegmenter[] array) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordsSegmenter>.CreateMarshalerArray(array, (o) => CreateMarshaler(o));
        public static (int length, IntPtr data) GetAbiArray(object box) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordsSegmenter>.GetAbiArray(box);
        public static unsafe global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordsSegmenter[] FromAbiArray(object box) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordsSegmenter>.FromAbiArray(box, FromAbi);
        public static (int length, IntPtr data) FromManagedArray(global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordsSegmenter[] array) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordsSegmenter>.FromManagedArray(array, (o) => FromManaged(o));
        public static void DisposeMarshaler(IObjectReference value) => MarshalInspectable.DisposeMarshaler(value);
        public static void DisposeMarshalerArray(MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordsSegmenter>.MarshalerArray array) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Data.Text.WordsSegmenter>.DisposeMarshalerArray(array);
        public static void DisposeAbi(IntPtr abi) => MarshalInspectable.DisposeAbi(abi);
        public static unsafe void DisposeAbiArray(object box) => MarshalInspectable.DisposeAbiArray(box);
    }
}
}
