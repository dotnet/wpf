// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Windows.Win32.System.Diagnostics.Debug;

namespace Windows.Win32.Foundation;

#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable format

/// <summary>
///  MIL error codes. Note that these codes use the same facility code as WIC.
/// </summary>
internal static class MilErrors
{
    // Copied from wgx_err.h

    private const int FACILITY_WGX = (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM;

    // MAKE_HRESULT definition:
    //
    // ((HRESULT) (((unsigned long)(sev)<<31) | ((unsigned long)(fac)<<16) | ((unsigned long)(code))) )
    private const int SuccessCode = (int)(((ulong)0 << 31) | ((ulong)(FACILITY_WGX) << 16));
    private const int ErrorCode = unchecked((int)(((ulong)1 << 31) | ((ulong)(FACILITY_WGX) << 16)));


    internal static readonly HRESULT WGXHR_CLIPPEDTOEMPTY           = (HRESULT)(SuccessCode | 1);
    internal static readonly HRESULT WGXHR_EMPTYFILL                = (HRESULT)(SuccessCode | 2);
    internal static readonly HRESULT WGXHR_INTERNALTEMPORARYSUCCESS = (HRESULT)(SuccessCode | 3);
    internal static readonly HRESULT WGXHR_RESETSHAREDHANDLEMANAGER = (HRESULT)(SuccessCode | 4);


    // Unique MIL error codes                   // Value: 0x8898xxxx -200330nnnn
    internal static readonly HRESULT WGXERR_OBJECTBUSY             = (HRESULT)(ErrorCode | 0x001);   //  4447
    internal static readonly HRESULT WGXERR_INSUFFICIENTBUFFER     = (HRESULT)(ErrorCode | 0x002);   //  4446
    internal static readonly HRESULT WGXERR_WIN32ERROR             = (HRESULT)(ErrorCode | 0x003);   //  4445
    internal static readonly HRESULT WGXERR_SCANNER_FAILED         = (HRESULT)(ErrorCode | 0x004);   //  4444
    internal static readonly HRESULT WGXERR_SCREENACCESSDENIED     = (HRESULT)(ErrorCode | 0x005);   //  4443
    internal static readonly HRESULT WGXERR_DISPLAYSTATEINVALID    = (HRESULT)(ErrorCode | 0x006);   //  4442
    internal static readonly HRESULT WGXERR_NONINVERTIBLEMATRIX    = (HRESULT)(ErrorCode | 0x007);   //  4441
    internal static readonly HRESULT WGXERR_ZEROVECTOR             = (HRESULT)(ErrorCode | 0x008);   //  4440
    internal static readonly HRESULT WGXERR_TERMINATED             = (HRESULT)(ErrorCode | 0x009);   //  4439
    internal static readonly HRESULT WGXERR_BADNUMBER              = (HRESULT)(ErrorCode | 0x00A);   //  4438
    internal static readonly HRESULT WGXERR_UNSUPPORTEDTEXTURESIZE = (HRESULT)(ErrorCode | 0x00B);   //  4437

    // An internal error (MIL bug) occurred. On checked builds, we would assert.
    internal static readonly HRESULT WGXERR_INTERNALERROR = (HRESULT)(ErrorCode | 0x080);   //  4320

    // This is a presentation error that is recoverable.  The caller needs
    // to reattempt present.
    // Known cause for this is another process calling PrintWindow on our hwnd
    // when we call UpdateLayeredWindow.
    internal static readonly HRESULT WGXERR_NEED_REATTEMPT_PRESENT = (HRESULT)(ErrorCode | 0x083);   //  4317

    // The display format we need to render is not supported by the
    // hardware device.
    internal static readonly HRESULT WGXERR_DISPLAYFORMATNOTSUPPORTED = (HRESULT)(ErrorCode | 0x084);   //  4316

    // A call to this method is invalid.
    internal static readonly HRESULT WGXERR_INVALIDCALL = (HRESULT)(ErrorCode | 0x085);   //  4315

    // Lock attempted on an already locked object.
    internal static readonly HRESULT WGXERR_ALREADYLOCKED = (HRESULT)(ErrorCode | 0x086);   //  4314

    // Unlock attempted on an unlocked object.
    internal static readonly HRESULT WGXERR_NOTLOCKED = (HRESULT)(ErrorCode | 0x087);   //  4313

    // No algorithm avaliable to render text with this device
    internal static readonly HRESULT WGXERR_DEVICECANNOTRENDERTEXT = (HRESULT)(ErrorCode | 0x088);   //  4312

    // Some glyph bitmaps, required for glyph run rendering, are not
    // contained in glyph cache.
    internal static readonly HRESULT WGXERR_GLYPHBITMAPMISSED = (HRESULT)(ErrorCode | 0x089);   //  4311

    // Some glyph bitmaps in glyph cache are unexpectedly big.
    internal static readonly HRESULT WGXERR_MALFORMEDGLYPHCACHE = (HRESULT)(ErrorCode | 0x08A);   //  4310

    // Marker error for known Win32 errors that are currently being ignored
    // by the compositor. This is to avoid returning S_OK when an error has occurred,
    // but still unwind the stack in the correct location
    internal static readonly HRESULT WGXERR_GENERIC_IGNORE = (HRESULT)(ErrorCode | 0x08B);   //  4309

    // Guideline coordinates are not sorted properly or contain NaNs.
    internal static readonly HRESULT WGXERR_MALFORMED_GUIDELINE_DATA = (HRESULT)(ErrorCode | 0x08C);   //  4308

    // No HW rendering device is available for this operation
    internal static readonly HRESULT WGXERR_NO_HARDWARE_DEVICE = (HRESULT)(ErrorCode | 0x08D);   //  4307

    // There has been a presentation error that may be recoverable. The caller
    // needs to recreate, rerender the entire frame, and reattempt present.
    // There are two known case for this:
    //  1) D3D Driver Internal error - should be investigated by DXG/IHV
    //  2) D3D E_FAIL
    //      a) Unknown root cause - should be investigated by DXG
    //      b) When resizing too quickly for DWM and D3D stay in sync
    internal static readonly HRESULT WGXERR_NEED_RECREATE_AND_PRESENT = (HRESULT)(ErrorCode | 0x08E);   //  4306

    // The object has already been initialized
    internal static readonly HRESULT WGXERR_ALREADY_INITIALIZED = (HRESULT)(ErrorCode | 0x08F);   //  4305

    // The size of the object does not match the expected size
    internal static readonly HRESULT WGXERR_MISMATCHED_SIZE = (HRESULT)(ErrorCode | 0x090);   //  4304

    // No Redirection surface avaiable
    internal static readonly HRESULT WGXERR_NO_REDIRECTION_SURFACE_AVAILABLE = (HRESULT)(ErrorCode | 0x091); //4303

    // Remoting of this content is not supported
    internal static readonly HRESULT WGXERR_REMOTING_NOT_SUPPORTED = (HRESULT)(ErrorCode | 0x092);   //  4302

    // Queued Presents are not being used
    internal static readonly HRESULT WGXERR_QUEUED_PRESENT_NOT_SUPPORTED = (HRESULT)(ErrorCode | 0x093);   //  4301

    // Queued Presents are not being used
    internal static readonly HRESULT WGXERR_NOT_QUEUING_PRESENTS = (HRESULT)(ErrorCode | 0x094);   //  4300

    // No redirection surface was available retry the call
    internal static readonly HRESULT WGXERR_NO_REDIRECTION_SURFACE_RETRY_LATER = (HRESULT)(ErrorCode | 0x095);  // 4299

    // Shader construction failed because it was too complex
    internal static readonly HRESULT WGXERR_TOOMANYSHADERELEMNTS = (HRESULT)(ErrorCode | 0x096);   //  4298

    // AVAILABLE        = (HRESULT)(ErrorCode | 0x097)   //  4297
    // AVAILABLE        = (HRESULT)(ErrorCode | 0x098)   //  4296

    // Shader compilation failed
    internal static readonly HRESULT WGXERR_SHADER_COMPILE_FAILED = (HRESULT)(ErrorCode | 0x099);   //  4295

    // Requested DX redirection surface size exceeded maximum texture size
    internal static readonly HRESULT WGXERR_MAX_TEXTURE_SIZE_EXCEEDED = (HRESULT)(ErrorCode | 0x09A);   //  4294

    // AVAILABLE                               = (HRESULT)(ErrorCode | 0x09B)   //  4293

    // Caps don't meet min WPF requirement for hw rendering
    internal static readonly HRESULT WGXERR_INSUFFICIENT_GPU_CAPS = (HRESULT)(ErrorCode | 0x09C); // 4292

    // Composition engine errors


    internal static readonly HRESULT WGXERR_UCE_INVALIDPACKETHEADER          = (HRESULT)(ErrorCode | 0x400);   //  3424
    internal static readonly HRESULT WGXERR_UCE_UNKNOWNPACKET                = (HRESULT)(ErrorCode | 0x401);   //  3423
    internal static readonly HRESULT WGXERR_UCE_ILLEGALPACKET                = (HRESULT)(ErrorCode | 0x402);   //  3422
    internal static readonly HRESULT WGXERR_UCE_MALFORMEDPACKET              = (HRESULT)(ErrorCode | 0x403);   //  3421
    internal static readonly HRESULT WGXERR_UCE_ILLEGALHANDLE                = (HRESULT)(ErrorCode | 0x404);   //  3420
    internal static readonly HRESULT WGXERR_UCE_HANDLELOOKUPFAILED           = (HRESULT)(ErrorCode | 0x405);   //  3419
    internal static readonly HRESULT WGXERR_UCE_RENDERTHREADFAILURE          = (HRESULT)(ErrorCode | 0x406);   //  3418
    internal static readonly HRESULT WGXERR_UCE_CTXSTACKFRSTTARGETNULL       = (HRESULT)(ErrorCode | 0x407);   //  3417
    internal static readonly HRESULT WGXERR_UCE_CONNECTIONIDLOOKUPFAILED     = (HRESULT)(ErrorCode | 0x408);   //  3416
    internal static readonly HRESULT WGXERR_UCE_BLOCKSFULL                   = (HRESULT)(ErrorCode | 0x409);   //  3415
    internal static readonly HRESULT WGXERR_UCE_MEMORYFAILURE                = (HRESULT)(ErrorCode | 0x40A);   //  3414
    internal static readonly HRESULT WGXERR_UCE_PACKETRECORDOUTOFRANGE       = (HRESULT)(ErrorCode | 0x40B);   //  3413
    internal static readonly HRESULT WGXERR_UCE_ILLEGALRECORDTYPE            = (HRESULT)(ErrorCode | 0x40C);   //  3412
    internal static readonly HRESULT WGXERR_UCE_OUTOFHANDLES                 = (HRESULT)(ErrorCode | 0x40D);   //  3411
    internal static readonly HRESULT WGXERR_UCE_UNCHANGABLE_UPDATE_ATTEMPTED = (HRESULT)(ErrorCode | 0x40E);   //  3410
    internal static readonly HRESULT WGXERR_UCE_NO_MULTIPLE_WORKER_THREADS   = (HRESULT)(ErrorCode | 0x40F);   //  3409
    internal static readonly HRESULT WGXERR_UCE_REMOTINGNOTSUPPORTED         = (HRESULT)(ErrorCode | 0x410);   //  3408
    internal static readonly HRESULT WGXERR_UCE_MISSINGENDCOMMAND            = (HRESULT)(ErrorCode | 0x411);   //  3407
    internal static readonly HRESULT WGXERR_UCE_MISSINGBEGINCOMMAND          = (HRESULT)(ErrorCode | 0x412);   //  3406
    internal static readonly HRESULT WGXERR_UCE_CHANNELSYNCTIMEDOUT          = (HRESULT)(ErrorCode | 0x413);   //  3405
    internal static readonly HRESULT WGXERR_UCE_CHANNELSYNCABANDONED         = (HRESULT)(ErrorCode | 0x414);   //  3404
    internal static readonly HRESULT WGXERR_UCE_UNSUPPORTEDTRANSPORTVERSION  = (HRESULT)(ErrorCode | 0x415);   //  3403
    internal static readonly HRESULT WGXERR_UCE_TRANSPORTUNAVAILABLE         = (HRESULT)(ErrorCode | 0x416);   //  3402
    internal static readonly HRESULT WGXERR_UCE_FEEDBACK_UNSUPPORTED         = (HRESULT)(ErrorCode | 0x417);   //  3401
    internal static readonly HRESULT WGXERR_UCE_COMMANDTRANSPORTDENIED       = (HRESULT)(ErrorCode | 0x418);   //  3400
    internal static readonly HRESULT WGXERR_UCE_GRAPHICSSTREAMUNAVAILABLE    = (HRESULT)(ErrorCode | 0x419);   //  3399
    internal static readonly HRESULT WGXERR_UCE_GRAPHICSSTREAMALREADYOPEN    = (HRESULT)(ErrorCode | 0x420);   //  3398
    internal static readonly HRESULT WGXERR_UCE_TRANSPORTDISCONNECTED        = (HRESULT)(ErrorCode | 0x421);   //  3397
    internal static readonly HRESULT WGXERR_UCE_TRANSPORTOVERLOADED          = (HRESULT)(ErrorCode | 0x422);   //  3396
    internal static readonly HRESULT WGXERR_UCE_PARTITION_ZOMBIED            = (HRESULT)(ErrorCode | 0x423);   //  3395



    // MIL AV Specific errors

    internal static readonly HRESULT WGXERR_AV_NOCLOCK                       = (HRESULT)(ErrorCode | 0x500);
    internal static readonly HRESULT WGXERR_AV_NOMEDIATYPE                   = (HRESULT)(ErrorCode | 0x501);
    internal static readonly HRESULT WGXERR_AV_NOVIDEOMIXER                  = (HRESULT)(ErrorCode | 0x502);
    internal static readonly HRESULT WGXERR_AV_NOVIDEOPRESENTER              = (HRESULT)(ErrorCode | 0x503);
    internal static readonly HRESULT WGXERR_AV_NOREADYFRAMES                 = (HRESULT)(ErrorCode | 0x504);
    internal static readonly HRESULT WGXERR_AV_MODULENOTLOADED               = (HRESULT)(ErrorCode | 0x505);
    internal static readonly HRESULT WGXERR_AV_WMPFACTORYNOTREGISTERED       = (HRESULT)(ErrorCode | 0x506);
    internal static readonly HRESULT WGXERR_AV_INVALIDWMPVERSION             = (HRESULT)(ErrorCode | 0x507);
    internal static readonly HRESULT WGXERR_AV_INSUFFICIENTVIDEORESOURCES    = (HRESULT)(ErrorCode | 0x508);
    internal static readonly HRESULT WGXERR_AV_VIDEOACCELERATIONNOTAVAILABLE = (HRESULT)(ErrorCode | 0x509);
    internal static readonly HRESULT WGXERR_AV_REQUESTEDTEXTURETOOBIG        = (HRESULT)(ErrorCode | 0x50A);
    internal static readonly HRESULT WGXERR_AV_SEEKFAILED                    = (HRESULT)(ErrorCode | 0x50B);
    internal static readonly HRESULT WGXERR_AV_UNEXPECTEDWMPFAILURE          = (HRESULT)(ErrorCode | 0x50C);
    internal static readonly HRESULT WGXERR_AV_MEDIAPLAYERCLOSED             = (HRESULT)(ErrorCode | 0x50D);
    internal static readonly HRESULT WGXERR_AV_UNKNOWNHARDWAREERROR          = (HRESULT)(ErrorCode | 0x50E);

    // Unused 0x60E - 0x61b

    // D3DImage specific errors
    internal static readonly HRESULT WGXERR_D3DI_INVALIDSURFACEUSAGE         = (HRESULT)(ErrorCode | 0x800);
    internal static readonly HRESULT WGXERR_D3DI_INVALIDSURFACESIZE          = (HRESULT)(ErrorCode | 0x801);
    internal static readonly HRESULT WGXERR_D3DI_INVALIDSURFACEPOOL          = (HRESULT)(ErrorCode | 0x802);
    internal static readonly HRESULT WGXERR_D3DI_INVALIDSURFACEDEVICE        = (HRESULT)(ErrorCode | 0x803);
    internal static readonly HRESULT WGXERR_D3DI_INVALIDANTIALIASINGSETTINGS = (HRESULT)(ErrorCode | 0x804);
}

#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore format
