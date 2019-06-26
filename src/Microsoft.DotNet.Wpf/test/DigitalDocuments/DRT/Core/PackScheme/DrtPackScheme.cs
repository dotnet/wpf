// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net;
using System.Net.Cache;         // for Cache Policy enums and class
using System.IO.Packaging;

// for reflection
using System.Reflection;
using System.Security;
using System.Security.Permissions;

namespace DRT
{
    /// <summary>
    /// PackScheme DRT class. All tests live as methods within this class.
    /// </summary>
    public sealed class PackSchemeTestHarness : DrtBase
    {
        [STAThread]
        public static int Main(string[] args)
        {
            DrtBase drt = new PackSchemeTestHarness();
            return drt.Run(args);
        }

        private PackSchemeTestHarness()
        {
            WindowTitle = PackSchemeTestSuite.Title;
            DrtName = PackSchemeTestSuite.Title;
            Suites = new DrtTestSuite[]
            {
                new PackSchemeTestSuite()
            };
        }
    }

    public sealed class PackSchemeTestSuite : DrtTestSuite
    {
        #region Statics
        public static string Title = "DrtPackScheme";
        static String file = Title + ".container";
        static String cacheFile = "cached_" + Title + ".container";

        // test specific
        System.Diagnostics.TraceListener trace = null;
        private System.Uri _realUri = null;
        private Int32 _timeout = -1;
        private FileInfo    _fileInfo = new FileInfo(file);
        private System.Uri  _uri;       // uri to _fileInfo
        private bool        _dump;      // extract contents intact to local file
        private bool        _stress;    // stress the reading logic

        static string[] partNames = new string[] {
            "coastal.xaml", 
            "okanagan.xaml", 
            "kootenay.xaml", 
            "/cities/vernon.jpg", 
            "/cities/kelowna.jpg", 
            "/cities/penticton.jpg", 
            "/cities/osoyoos.jpg",
        };
        static string[] contentTypes = new string[] {
            "text/xml", "text/xaml", "text/xml", "image/jpeg", "image/jpeg", "image/jpeg", "image/jpeg"
        };

        #endregion

        public PackSchemeTestSuite() : base(Title)
        {
        }

        public override DrtTest[] PrepareTests()
        {
            // register the pack scheme
            RegisterScheme();

            // create a container
            EnsureContainer();

            // return the lists of tests to run against the tree
            return new DrtTest[]
            {
                new DrtTest( Exercise ),
                new DrtTest( RegressionTests ),
                new DrtTest( CachePolicyTests ),
                new DrtTest( PackageStoreTests ),
            };
        }

        public override void PrintOptions()
        {
            // general options
            base.PrintOptions();

            Console.WriteLine("\n\nPackScheme options:");
            Console.WriteLine("  -uri URI          hit container on real server - not recommended for live DRT usage");
            Console.WriteLine("  -timeout N        use specified timeout in milliseconds");
        }

        /// <summary>
        /// Override this in derived classes to handle command-line arguments one-by-one.
        /// </summary>
        /// <param name="arg">current argument</param>
        /// <param name="option">if there was a leading "-" or "/" to arg</param>
        /// <param name="args">the array of command line arguments</param>
        /// <param name="k">current index in the argument array.  passed by ref so you can increase it to "consume" arguments to options.</param>
        /// <returns>True if handled</returns>
        public override bool HandleCommandLineArgument(string arg, bool option, string[] args, ref int k)
        {
            if (option)
            {
                switch (arg)
                {
                    case "uri":
                        // Register our prefix with Uri libraries
                        Console.WriteLine("Register PACK prefix with Uri factory");
                        PackUriHelper.Create(new Uri("http://default"));     // hack to ensure pack scheme is registered Uri

                        String uri = args[++k];
                        if (uri.StartsWith("pack"))
                        {
                            Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out _realUri);
                        }
                        else
                            _realUri = PackUriHelper.Create(new Uri(uri));

                        break;

                    case "timeout":
                        if (!Int32.TryParse(args[++k], out _timeout))
                            _timeout = -1;
                        break;

                    case "trace":
                        trace = new System.Diagnostics.ConsoleTraceListener();
                        System.Diagnostics.Trace.Listeners.Clear();
                        System.Diagnostics.Trace.Listeners.Add(trace);
                        break;

                    case "dump":
                        _dump = true;
                        break;

                    case "stress":
                        _stress = true;
                        break;

                    // we don't recognize so pass to base class for default handling
                    default:
                        return base.HandleCommandLineArgument(arg, option, args, ref k);
                }

                return true;
            }

            return false;
        }

        public void RegisterScheme()
        {
            // Register our scheme with WebRequest
            Console.WriteLine("Register PACK scheme with WebRequest");
            DRT.Assert(WebRequest.RegisterPrefix("pack", new PackWebRequestFactory()), "RegisterPrefix failed");

            // Need to do this once per AppDomain (per alexeiv)   
            (new SecurityPermission(SecurityPermissionFlag.ControlPrincipal)).Assert();
            try
            {
                AuthenticationManager.CredentialPolicy = new Microsoft.Win32.IntranetZoneCredentialPolicy();
            }
            finally
            {
                CodeAccessPermission.RevertAssert();
            }

        }

        public void EnsureContainer()
        {
            if (!_fileInfo.Exists)
            {
                int i = 0;
                Package c = Package.Open(_fileInfo.FullName, FileMode.CreateNew);
                for (i = 0; i < partNames.Length; i++)
                {
                    c.CreatePart(PackUriHelper.CreatePartUri(new Uri(partNames[i], UriKind.Relative)), contentTypes[i]);
                }

                // add some content
                PackagePartCollection parts = c.GetParts();
                foreach (PackagePart p in parts)
                {
                    using (Stream s = p.GetStream())
                    {
                        using (StreamWriter writer = new StreamWriter(s))
                        {
                            writer.Write("This is a stream named: ");
                            writer.Write(p.Uri.ToString());
                            for (i = 0; i < 10000; i++)
                                writer.Write(i + " Starting on May 10, 2004, all vehicles parked on the Microsoft�s Puget Sound, Las Colinas, Silicon Valley, Charlotte and Fargo campuses must display the new, yellow parking permit. Registraton/Renewal instructions enclosed. Any vehicle that is not validated and assigned a new yellow permit by close of business May 14, 2004 will automatically be deleted from the registration database.");
                            writer.Flush();
                        }
                    }
                }

                c.Close();
            }

            // create the URI
            Uri fileUri = PackUriHelper.Create(new Uri(_fileInfo.FullName));
            if (_realUri == null)
            {
                _uri = fileUri;
            }
            else
                _uri = _realUri;
        }

        #region RegressionTests

        // PS992484 - Cached PackWebRequest: if the second request is a cache-hit, 
        // but it is  made to get a full container (instead of a single part), we should 
        // use the standard non-cached PackWebRequest instead of throwing an ObjectNull Exp
        public void PS992484(Uri uri)
        {
            // create a container and poke it into the container
            _fileInfo = new FileInfo(Path.GetTempFileName());
            if (_fileInfo.Exists)
                _fileInfo.Delete();

            Uri partUri = PackUriHelper.CreatePartUri(new Uri("/partOne.junk", UriKind.Relative));
            Package c = Package.Open(_fileInfo.FullName, FileMode.CreateNew);
            PackagePart p = c.CreatePart(partUri, "abc123/xyz");

            // add some content
            using (Stream s = p.GetStream())
            {
                using (StreamWriter writer = new StreamWriter(s))
                {
                    writer.Write(p.Uri.ToString());
                    writer.Write(" By STEVEN CHASE and SIMON AVERY - From Friday's Globe and Mail - Canadian authorities" +
                    "say they will continue to prohibit cellphone calls during flights on this country's carriers, despite moves by the" +
                    " United States to reconsider such a ban. But Canadians who need to be connected in flight can take heart: There" +
                    " is faint hope that they might be able to surf the Internet wirelessly if Ottawa is convinced it's safe.");
                    writer.Flush();
                }
            }
            c.Close();

            // make a copy and open the package on that copy - this avoids sharing violations
            // which happen because these are actually FILE Uri's.  If we had a reliable http server
            // we could hit, this would not be an issue because the target file and the cached file
            // would be different.
            FileInfo fi = new FileInfo(cacheFile);
            if (fi.Exists)
                fi.Delete();
            File.Copy(_fileInfo.FullName, cacheFile);

            c = Package.Open(cacheFile, FileMode.Open, FileAccess.Read);
/*
            Assembly assembly = Assembly.GetAssembly(typeof(PackWebRequest)); // to get PresentationCore.dll
            Type packageCacheType = assembly.GetType("MS.Internal.IO.Packaging.PreloadedPackages");
            MethodInfo addMethod = packageCacheType.GetMethod("AddPackage", BindingFlags.NonPublic | BindingFlags.Static);
*/
            // now add the container to the cache
            Uri fileUri = new Uri(_fileInfo.FullName);
            Uri packUri = PackUriHelper.Create(fileUri, partUri);               // uri to the Part in the package
            Uri packUriNoPart = PackUriHelper.Create(fileUri);                  // uri to the entire package
//            addMethod.Invoke(null, new object[] { PackUriHelper.GetPackageUri(packUriNoPart), c });
            AddToPreloadedCache(PackUriHelper.GetPackageUri(packUriNoPart), c);
            
            // request for part should hit the cache
            WebRequest request = WebRequest.Create(packUri);
            WebResponse response;
            using (response = request.GetResponse())
            {
                DRT.Assert((Boolean)response.IsFromCache, "Part WebRequest should have hit the cache - error");
                Console.WriteLine("\tIsFromCache= " + (Boolean)response.IsFromCache);
            }            

            // request for wholc container should bypass the cache
            request = WebRequest.Create(packUriNoPart);
            using (response = request.GetResponse())
            {
                DRT.Assert(!(Boolean)response.IsFromCache, "Full Package WebRequest should have bypassed the cache - error");
                Console.WriteLine("\tIsFromCache= " + (Boolean)response.IsFromCache);
            }

            // cleanup
            ClearPreloadedCache();
            c.Close();

            fi.Delete();
            _fileInfo.Delete();
        }

#if false   // this failure should occur as the lifetime of the stream and the response are intertwined
        // PS992496 - GetResponseStream.Close() will cause failure in ContentLength when
        // operating on Cached response.
        //
        // Analysis: This was caused by the fact that we returned the PackagePart stream directly without wrapping it in\
        // a ResponseStream() object.
        //
        public void PS992496(Uri uri)
        {
            Console.WriteLine("PS992496 with Uri: " + uri);
            long length1 = 0;
            long length2 = 0;
            {
                // create request and get response
                WebRequest request = WebRequest.Create(uri);
                WebResponse response = request.GetResponse();
                Console.WriteLine("\tIsFromCache= " + (Boolean)response.IsFromCache);

                // GetResponseStream
                Stream s = response.GetResponseStream();

                // Read from Stream
                s.ReadByte();

                // Close stream
                s.Close();

                // Access Reponse. ContentLength >> throw's here
                length1 = response.ContentLength;

                // clean up and return
                response.Close();
            }

            // now do these in reverse order to exercise the logic in ContentLength for the cached item
            {
                // create request and get response
                WebRequest request = WebRequest.Create(uri);
                WebResponse response = request.GetResponse();

                // Access Reponse. ContentLength >> throw's here
                length2 = response.ContentLength;

                // GetResponseStream
                Stream s = response.GetResponseStream();

                // Read from Stream
                s.ReadByte();

                // Close stream
                s.Close();

                // clean up and return
                response.Close();
            }

            // verify
            DRT.Assert(length1 == length2, "Different Lengths in test PS992496.  " + length1 + " != " + length2);
        }
#endif

        // PS: 908905 Close a ResponseStream (or the PackWebResponse) on a 
        // PackWebRequest without exercising the responsed stream will throw 
        // an ApplicationException
        public void PS908905(Uri uri)
        {
            Console.WriteLine("\nPS908905 with Uri: " + uri);

            // create request but don't exercise
            WebRequest request = WebRequest.Create(uri);
            WebResponse response = request.GetResponse();

            response.Close();
        }

        // PS: 908596 Dispose/Close in PackWebRequest and PackWebResponse behave 
        // differently from the standard WebRequest and WebResponse
        public void PS908596(Uri uri)
        {
            Console.WriteLine("\nPS908596 with Uri: " + uri);
            Console.WriteLine("\tCase 1: Close stream first");

            // create and exercise
            WebRequest request = WebRequest.Create(uri);
            WebResponse response = request.GetResponse();
            Stream s = response.GetResponseStream();

            s.ReadByte();
            s.Close();
            response.Close();

            Console.WriteLine("\tCase 2: Close response first");

            // create and exercise
            request = WebRequest.Create(uri);
            response = request.GetResponse();
            s = response.GetResponseStream();

            s.ReadByte();

            response.Close();
            s.Close();
        }

        /// <summary>
        /// PS914087 CompoundFileWebResponse.ContentType will throw ObjectNULLExp
        /// </summary>
        /// <param name="uri"></param>
        public void PS914087(Uri uri)
        {
            Console.WriteLine("\nPS914087 with Uri: " + uri);

            // create and exercise
            WebRequest request = WebRequest.Create(uri);
            WebResponse response = request.GetResponse();

            // access ContentType directly
            try
            {
                String s = response.ContentType;

                Console.WriteLine("\tContentType= " + s);
            }
            catch (NotImplementedException)
            {
                Console.WriteLine("\tContentType not implemented for this type: " + response.GetType().ToString());
            }

            long length = response.ContentLength;
            Console.WriteLine("\tContentLength= " + length);

            response.Close();
        }

        /// <summary>
        /// PS907025 Missing public properties in CompoundFileWebResponse class
        /// </summary>
        /// <param name="uri"></param>
        public void PS907025(Uri uri)
        {
            Console.WriteLine("\nPS907025 with Uri: " + uri);

            // create and exercise
            WebRequest request = WebRequest.Create(uri);
            WebResponse response = request.GetResponse();

            // ensure properties are available
            WebHeaderCollection headers = response.Headers;
            Uri responseUri = response.ResponseUri;
            Console.WriteLine("\tFromCache: " + response.IsFromCache + "\n\tHeaders= " + headers.Count.ToString() + "\n\tResponseUri= " + responseUri + "\n");
            response.Close();
        }

        /// <summary>
        /// PS1600038 Closing WebResponse should close Stream (but closing Stream should not close WebResponse)
        /// </summary>
        /// <param name="uri"></param>
        public void PS1600038(Uri uri)
        {
            Console.WriteLine("\nPS1600038 with Uri: " + uri);
            Console.WriteLine("\tCase 1: Close stream first");

            // create and exercise
            WebRequest request = WebRequest.Create(uri);
            WebResponse response = request.GetResponse();
            Stream s = response.GetResponseStream();

            s.ReadByte();
            s.Close();

            // verify that response is never closed by closing the stream
            bool exceptionCaught = false;
            try
            {
                response.GetResponseStream();
            }
            catch (ObjectDisposedException)
            {
                exceptionCaught = true;
            }

            DRT.Assert(!exceptionCaught, "Caught unexpected exception - Closing ResponseStream should not close the WebResponse object");
            response.Close();

            Console.WriteLine("\tCase 2: Close response first");

            // create and exercise
            request = WebRequest.Create(uri);
            response = request.GetResponse();
            s = response.GetResponseStream();

            s.ReadByte();

            response.Close();

            // verify that stream is closed by forcing an ObjectDisposedException
            exceptionCaught = false;
            try
            {
                s.ReadByte();
            }
            catch (ObjectDisposedException)
            {
                exceptionCaught = true;
            }
            DRT.Assert(exceptionCaught, "Failed to catch expected exception - Closing WebResponse did not close the Stream object");

            s.Close();
        }

        /// <summary>
        /// Prevent regressions
        /// </summary>
        public void RegressionTests()
        {
            // choose a Uri - fallback to fileUri if no uri passed on command line (DRT case)
            Uri uri;
            if (_realUri != null)
                uri = _realUri;         // caller specified "real" uri - don't use for live DRT's
            else
                uri = _uri;

            RegressionTests(uri);
        }

        /// <summary>
        /// Prevent regressions
        /// </summary>
        public void RegressionTests(Uri uri)
        {
            Console.WriteLine("\nRunning Regression Tests...");
            //            PS992496(uri);    - bogus bug - closing Stream also closes Response
            PS908905(uri);
            PS908596(uri);
            PS914087(uri);
            PS907025(uri);
            PS992484(uri);
            PS1600038(uri);
        }

#endregion


        public void Exercise()
        {
            Uri fileUri = PackUriHelper.Create(new Uri(_fileInfo.FullName));

            System.Uri uri;
            if (_realUri == null)
            {
                uri = fileUri;
            }
            else
                uri = _realUri;

            Console.WriteLine("Working against container: " + uri.ToString());

            // work against entire container (no part) - but not from the cache because it does not support full-container streams
            RunRequest(uri);
        }

        public void RunRequest(Uri uri)
        {
            ExerciseTheWebRequest(uri, _dump, true, false);
            ExerciseTheWebRequest(uri, _dump, false, true);
        }

        private void PackageStoreTests()
        {
            // TODO: create and query a test package for this test that is guaranteed to not already be
            // in the Preloaded package cache
            String fileName = "./PackageStoreTest.package";
            FileInfo fi = new FileInfo(fileName);
            Package p = Package.Open(fi.FullName, FileMode.Create, FileAccess.ReadWrite);
            Uri packageUri = new Uri(fi.FullName, UriKind.Absolute);
            Uri partUri = new Uri("/booger_nugget", UriKind.Relative);
            p.CreatePart(PackUriHelper.CreatePartUri(partUri), "solitaire/blackjack");
            p.Flush();

            // close and re-open in sharable mode
            p.Close();
            p = Package.Open(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

            Uri uri = PackUriHelper.Create(packageUri, partUri);

            // try without Package in the Store
            ExerciseTheWebRequest(uri, _dump, true, false);

            // and then try with Package in the Store
            PackageStore.AddPackage(packageUri, p);
            ExerciseTheWebRequest(uri, _dump, true, false);

            // try regressions with CachedResponse
            RegressionTests(uri);

            PackageStore.RemovePackage(uri);

            // clean up
            p.Close();
            fi.Delete();
        }

        private void CachePolicyTests()
        {
            // TODO: create and query a test package for this test that is guaranteed to not already be
            // in the Preloaded package cache
            String fileName = "./my package.package";
            FileInfo fi = new FileInfo(fileName);
            Package p = Package.Open(fi.FullName, FileMode.Create, FileAccess.ReadWrite);
            Uri packageUri = new Uri(fi.FullName, UriKind.Absolute);
            Uri partUri = new Uri("/silly_willy", UriKind.Relative);
            p.CreatePart(PackUriHelper.CreatePartUri(partUri), "snufflufagus/contenttype");
            p.Flush();

            // close and re-open in sharable mode
            p.Close();
            p = Package.Open(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

            Uri uri = PackUriHelper.Create(packageUri, partUri);

            // try without cache
            CachePolicyDriver(uri, false);

            // and then try with package in the cache
            PackageStore.AddPackage(packageUri, p);
            CachePolicyDriver(uri, true);
            PackageStore.RemovePackage(uri);

            // clean up
            p.Close();
            fi.Delete();
        }

        private void CachePolicyDriver(Uri uri, bool packageActuallyInCache)
        {
            // cache tests that could succeed
            bool defaultUsesCache = WebRequest.DefaultCachePolicy.Level != RequestCacheLevel.BypassCache;
            ExerciseCachePolicy(uri, RequestCacheLevel.Default, defaultUsesCache, defaultUsesCache);
            ExerciseCachePolicy(uri, RequestCacheLevel.BypassCache, false, true);
            ExerciseCachePolicy(uri, RequestCacheLevel.CacheOnly, packageActuallyInCache, packageActuallyInCache);
            ExerciseCachePolicy(uri, RequestCacheLevel.CacheIfAvailable, packageActuallyInCache, true);

            // these should always fail since they are deemed invalid options
            ExerciseCachePolicy(uri, RequestCacheLevel.Revalidate, false, false);
            ExerciseCachePolicy(uri, RequestCacheLevel.Reload, false, false);
            ExerciseCachePolicy(uri, RequestCacheLevel.NoCacheNoStore, false, false);
        }

        public void ExerciseCachePolicy(Uri uri, RequestCacheLevel level, bool shouldHitCache, bool shouldSucceed )
        {
            Console.WriteLine("\nExercise: " + uri + " for Cache policy: " + level.ToString());

            try
            {
                WebRequest request = WebRequest.Create(uri);
                request.CachePolicy = new RequestCachePolicy(level);
                WebResponse response = request.GetResponse();
                DRT.Assert(response != null, "GetResponse returned NULL!");

                // cache hit?
                DRT.Assert(shouldHitCache == response.IsFromCache, "Cache " + (shouldHitCache ? "miss" : "hit") + " not expected.");

                PackWebResponse pResponse = response as PackWebResponse;
                DRT.Assert(pResponse != null, "GetResponse returned a WebResponse that was not a PackWebResponse.");

                response.Close();
            }
            catch (WebException e)
            {
                Console.WriteLine((shouldSucceed ? "Unexpected" : "Expected ") + " WebException caught: " + e.Message);
                DRT.Assert(!shouldSucceed, "PackScheme: CacheLevel " + level.ToString() + " should have succeeded");
            }
        }
        
        public void ExerciseTheWebRequest(Uri uri, bool dump, bool readFirstK, bool randomRead)
        {
            Console.WriteLine("\nExercise Request: " + uri);

            WebRequest request = WebRequest.Create(uri);
            if (_timeout != -1 && (request is PackWebRequest))
            {
                Console.WriteLine("DRT: Setting Timeout: {0}", _timeout);
                ((PackWebRequest)request).Timeout = _timeout;
            }

            // don't let the WinInet cache mess up our efforts to exercise NetStream
            ((PackWebRequest)request).GetInnerRequest().CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.BypassCache);

            // poke around at some properties to ensure support from cached and non-cached scenarios
            ExerciseRequestProperties(request);

            WebResponse response = request.GetResponse();
            DRT.Assert(response != null, "GetResponse returned NULL!");

            // cache hit?
            if (response.IsFromCache)
            {
                Console.WriteLine("Cache Hit. Request satisfied from Loader object cache.");
            }

            PackWebResponse pResponse = response as PackWebResponse;
            DRT.Assert(pResponse != null, "GetResponse returned a WebResponse that was not a PackWebResponse.");

            Stream s = response.GetResponseStream();
            DRT.Assert(s != null, "GetResponseStream returned NULL!");

            // extract from the stream
            int offset = 0;
            int count = 1024;
            byte[] buf = new byte[0x1000];

            if(s.CanSeek)
            {
                s.Seek(0, SeekOrigin.Begin);
            }

            long read = 0;

            // random reading
            if (randomRead)
            {
                Console.WriteLine("\tRandom Read");

                // exercise the byteRangeDownloader and block merging logic
                int runs = 1000;
                int maxLength = (int)s.Length;
                int searchPortion = maxLength/2;                // only search in the last portion
                System.Random rand = new System.Random(1234);   // fixed seed for reproducible results
                buf = new byte[Math.Max(maxLength, buf.Length)];                      // we need a LOT more room for this test
                while (runs > 0)
                {
                    // at least 1 byte but not too big because the BufferStream ends up issuing maximum read sizes
                    // of 65536 bytes
                    count = rand.Next(searchPortion - 2) + 1;                            

                    // only access the latter portion to make sure we exercise the byte-ranges as much as possible
                    offset = rand.Next(searchPortion - count - 1) + maxLength - searchPortion;     
                    s.Seek(offset, SeekOrigin.Begin);
                    if ((read = s.Read(buf, 0, count)) == 0)
                        continue;

                    // read the next "blocks" to ensure some merging activity (all calls get mapped to 512 byte blocks
                    int toRead = Math.Min(500, maxLength);
                    for (int j = 0; j < 20; j++)
                    {
                        // by StorageRoot)
                        if (maxLength - offset > j * toRead)
                        {
                            if ((read = s.Read(buf, 0, toRead)) == 0)
                                break;
                        }
                    }
                    --runs;
                }
            }
            if (_stress)
            {
                int blockLength = 0x1000;
                long index = s.Length;

                // jump around a bit to test off-by-one errors
                int[] queries = new int[] { blockLength, blockLength - 1, blockLength + 1, blockLength - 2, blockLength + 2 };
                int queryIndex = 0;
                while (true)
                {
                    index = Math.Max(index - queries[queryIndex], 0);
                    // This will fail if it is run on a ZipWrappingStream (_basestream = SubReadStream) as the stream does not support seeking
                    s.Seek(index, SeekOrigin.Begin);

                    // vary the amount read
                    if ((read = s.Read(buf, offset, (s.Position % 2 > 0) ? blockLength : queryIndex)) == 0)
                        break;

                    if (index == 0)
                        break;

                    // next index
                    queryIndex++;
                    if (queryIndex >= queries.Length)
                        queryIndex = 0;
                }
            }
            if (dump)  // read and dump
            {
                string fileName = uri.GetLeftPart(UriPartial.Path);
                fileName = fileName.Substring(fileName.LastIndexOf('/'));
                string localName = @".\" + fileName + ".stream";
                FileStream fs = new FileStream(localName, FileMode.Create);

                int blockLength = 0x1000;
                long index = s.Length;
                while (true)
                {
                    index = Math.Max(index - blockLength, 0);
                    // This will fail if it is run on a ZipWrappingStream (_basestream = SubReadStream) as the stream does not support seeking
                    s.Seek(index, SeekOrigin.Begin);
                    fs.Seek(index, SeekOrigin.Begin);

                    if ((read = s.Read(buf, 0, blockLength)) > 0)
                        fs.Write(buf, 0, (int)read);
                    else
                        break;

                    if (index == 0)
                        break;
                }
                fs.Close();
            }
            else
            {
                Console.WriteLine(readFirstK ? "\tRead 1K only" : "\tRead Full");

                // read but no write
                long total = 0;

                while ((read = s.Read(buf, offset, count)) > 0)
                {
                    total += read;
                    if (readFirstK && total >= 1024)
                        break;
                }
            }

            // clean up
            s.Close();
            response.Close();
        }

        // helper to add a package to the Preloaded PackageCache
        private void AddToPreloadedCache(Uri uri, Package p)
        {
            Assembly assembly = Assembly.GetAssembly(typeof(PackWebRequest)); // to get PresentationCore.dll
            Type packageCacheType = assembly.GetType("MS.Internal.IO.Packaging.PreloadedPackages");
            MethodInfo addMethod = packageCacheType.GetMethod(
                "AddPackage", 
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new Type[] { typeof(System.Uri), typeof(System.IO.Packaging.Package), typeof(bool) },
                null);
            addMethod.Invoke(null, new object[] { uri, p, false }); // never thread-safe
        }

        // helper to remove a package from the Preloaded PackageCache
        private void ClearPreloadedCache()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(PackWebRequest)); // to get PresentationCore.dll
            Type packageCacheType = assembly.GetType("MS.Internal.IO.Packaging.PreloadedPackages");
            MethodInfo addMethod = packageCacheType.GetMethod(
                "Clear",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new Type[] { },
                null);
            addMethod.Invoke(null, new object[] { }); // never thread-safe
        }

        private void ExerciseRequestProperties(WebRequest request)
        {
            Console.WriteLine("\n\nExerciseRequestProperties - before");
            DumpRequest(request);

            // squirrel away into local vars
            RequestCachePolicy rPolicy = request.CachePolicy;
            String rName = request.ConnectionGroupName;
            long contentLength = request.ContentLength;
            String contentType = null;
            try
            {
                contentType = request.ContentType;
            }
            catch (NotSupportedException)
            {
                Console.WriteLine("ContentType not supported by current protocol handler");
            }

            ICredentials credentials = request.Credentials;
            String method = request.Method;
            int timeOut = request.Timeout;

            // modify
            request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
            request.ConnectionGroupName = "MyConnectionGroupName";
            if (contentType != null)            // only set if we didn't get an exception above
                request.ContentType = "MyContent/Type";

            try
            {
                request.Method = "MyMethod";
            }
            catch (NotSupportedException)
            {
                Console.WriteLine("Method not supported by current protocol handler");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Method not supported by current protocol handler");
            }
            request.Timeout = 31459;
            request.Credentials = new NetworkCredential("UserName", "Password");

            Console.WriteLine("ExerciseRequestProperties - after");
            DumpRequest(request);

            // restore
            Console.WriteLine("Restoring original values\n\n");
            request.CachePolicy = rPolicy;
            request.ConnectionGroupName = rName;
//            request.ContentLength = contentLength;        // cannot set by contract
            if (contentType != null)            // only set if we didn't get an exception above
                request.ContentType = contentType;
            request.Credentials = credentials;
            request.Method = method;
            request.Timeout = timeOut;
        }

        private void DumpRequest(WebRequest request)
        {
            Console.WriteLine("\n<< Dump WebRequest properties:{0} >>", ((PackWebRequest)request).GetInnerRequest().GetType().ToString());
            Console.WriteLine("\tCachePolicy:\t{0}", request.CachePolicy.ToString());
            Console.WriteLine("\tConnectionGroupName:{0}", request.ConnectionGroupName == null ? "<null>" : request.ConnectionGroupName);
            Console.WriteLine("\tContentLength:\t{0}", request.ContentLength.ToString());
            try
            {
                Console.WriteLine("\tContentType:\t{0}", request.ContentType == null ? "<null>" : request.ContentType);
            }
            catch (NotSupportedException)
            {
                Console.WriteLine("\tContentType: <NotSupportedException>");
            }
            Console.WriteLine("\tCredentials:\t{0}", request.Credentials == null ? "<null>" : request.Credentials.ToString());
            Console.WriteLine("\tHeaders:{0}", request.Headers.Count == 0 ? "<empty>" : "");
            foreach (string s in request.Headers)
                Console.WriteLine("\t" + s);
            Console.WriteLine("\tImpersonationLevel:{0}", request.ImpersonationLevel.ToString());
            Console.WriteLine("\tMethod:\t\t{0}", request.Method.ToString());
            try
            {
                Console.WriteLine("\tPreAuthenticate:{0}", request.PreAuthenticate.ToString());
            }
            catch (NotSupportedException)
            {
                Console.WriteLine("\tPreAuthenticate: <NotSupportedException>");
            }
            Console.WriteLine("\tProxy:\t\t{0}", request.Proxy == null ? "<null>" : request.Proxy.ToString());
            Console.WriteLine("\tRequestUri:\t{0}", request.RequestUri.ToString());
            Console.WriteLine("\tTimeout:\t{0}", request.Timeout.ToString());
            try
            {
                Console.WriteLine("\tUseDefaultCredentials:", request.UseDefaultCredentials.ToString());
            }
            catch (NotSupportedException)
            {
                Console.WriteLine("\tUseDefaultCredentials: <NotSupportedException>");
            }
            catch (NotImplementedException)
            {
                Console.WriteLine("\tUseDefaultCredentials: <NotImplementedException>");
            }
            Console.WriteLine("<< End Dump >>\n");  
        }
    }
}

