using System.Runtime.InteropServices;
using ClrDebug;

namespace NodeDev.Core.Debugger;

/// <summary>
/// IMetadataImport interface for querying assembly metadata
/// </summary>
[ComImport]
[Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMetadataImport
{
	void CloseEnum(IntPtr hEnum);
	
	HRESULT CountEnum(IntPtr hEnum, out uint pulCount);
	
	void ResetEnum(IntPtr hEnum, uint ulPos);
	
	HRESULT EnumTypeDefs(
		ref IntPtr phEnum,
		[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
		uint[] rTypeDefs,
		uint cMax,
		out uint pcTypeDefs);
	
	HRESULT EnumInterfaceImpls(
		ref IntPtr phEnum,
		uint td,
		[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]
		uint[] rImpls,
		uint cMax,
		out uint pcImpls);
	
	HRESULT EnumTypeRefs(
		ref IntPtr phEnum,
		[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
		uint[] rTypeRefs,
		uint cMax,
		out uint pcTypeRefs);
	
	HRESULT FindTypeDefByName(
		[MarshalAs(UnmanagedType.LPWStr)] string szTypeDef,
		uint tkEnclosingClass,
		out uint ptd);
	
	// Many more methods exist, but we only need these for now
	HRESULT GetScopeProps(
		[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
		char[] szName,
		uint cchName,
		out uint pchName,
		out Guid pmvid);
	
	HRESULT GetModuleFromScope(out uint pmd);
	
	HRESULT GetTypeDefProps(
		uint td,
		[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
		char[] szTypeDef,
		uint cchTypeDef,
		out uint pchTypeDef,
		out uint pdwTypeDefFlags,
		out uint ptkExtends);
	
	HRESULT GetInterfaceImplProps(
		uint iiImpl,
		out uint pClass,
		out uint ptkIface);
	
	HRESULT GetTypeRefProps(
		uint tr,
		out uint ptkResolutionScope,
		[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]
		char[] szName,
		uint cchName,
		out uint pchName);
	
	HRESULT EnumMembers(
		ref IntPtr phEnum,
		uint cl,
		[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]
		uint[] rMembers,
		uint cMax,
		out uint pcTokens);
	
	HRESULT EnumMethods(
		ref IntPtr phEnum,
		uint cl,
		[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]
		uint[] rMethods,
		uint cMax,
		out uint pcTokens);
	
	HRESULT GetMethodProps(
		uint mb,
		out uint pClass,
		[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]
		char[] szMethod,
		uint cchMethod,
		out uint pchMethod,
		out uint pdwAttr,
		out IntPtr ppvSigBlob,
		out uint pcbSigBlob,
		out uint pulCodeRVA,
		out uint pdwImplFlags);
}
