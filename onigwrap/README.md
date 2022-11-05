# Onigwrap

Provides interop between Oniguruma and dotnet managed code.

Based on [fluentCODE/onigwrap](https://github.com/fluentCODE/onigwrap)

* `onigwrap.c` - Is a C library that wraps Oniguruma, greatly simplifying the interface for which we need to provide interop. This also greatly limits the flexibility of Onig, but for our use of this library, we didn't need any of that flexibility.
## Building

First, get a copy of Oniguruma.

* https://github.com/kkos/oniguruma

Copy `oniguruma.h` into the `src` folder, alongside onigwrap.c and onigwrap.h.

From here, the build steps diverge for each platform:

Mac
---

1. Compile oniguruma targeting both x86_64 and arm64:
```
make distclean
autoreconf -vfi
./configure CC="gcc -arch x86_64 -arch arm64"
make 
```
Copy `$libs/libonig.a` and `src/oniguruma.h` to the onigwrap folder:
```
cp src/.libs/libonig.a ../TextMateSharp/onigwrap/src
cp src/oniguruma.h ../TextMateSharp/onigwrap/src
```

Now we build onigwrap:

2. Compile onigwrap in the following way:

```
cd ../TextMateSharp/onigwrap/src
clang -target x86_64-apple-macos10.12 -dynamiclib -L. -lonig -o x86_libonigwrap.dylib onigwrap.c
clang -target arm64-apple-macos11 -dynamiclib -L. -lonig -o arm_libonigwrap.dylib onigwrap.c 
lipo -create -output libonigwrap.dylib x86_libonigwrap.dylib arm_libonigwrap.dylib
```
3. Ensure that the library has the correct archs:
```
$ lipo -archs libonigwrap.dylib
x86_64 arm64
```

Take the onigwrap.dylib and put it alongside your binary:
```
cp libonigwrap.dylib ../../src/TextMateSharp/Internal/Oniguruma/Native/osx/
```

Windows
-------
* Setup and install C++ Build [Tools for Visual Studio](https://visualstudio.microsoft.com/es/thank-you-downloading-visual-studio/?sku=BuildTools&rel=16).
* Open a console and setup the Visual C compiler env vars, according to your platform for example:

`"C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\VC\Auxiliary\Build\vcvars64.bat"`

Then build and configure oniguruma [following the instructions](https://github.com/kkos/oniguruma#case-3-windows-6432bit-platform-visual-studio) on the Oniguruma repository.

Copy `onig_s.lib` and `oniguruma.h` to the `src` folder.
```
copy onig_s.lib ..\..\source\repos\TextMateSharp\onigwrap\src
copy src\oniguruma.h ..\..\source\repos\TextMateSharp\onigwrap\src
```

Build onigwrap:

`cl.exe /DONIG_EXTERN=extern /D_USRDLL /D_WINDLL onigwrap.c /link onig_s.lib /DLL /OUT:onigwrap.dll`

Copy onigwrap.dll to the folder with your binary.
```
copy onigwrap.dll ..\..\src\TextMateSharp\Internal\Oniguruma\Native\win-x64
```

Repeat the same process for x86 platform:

Linux
-----

Build and configure oniguruma [following the instructions](https://github.com/kkos/oniguruma#case-2-manual-compilation-on-linux-unix-and-cygwin-platform) on the Oniguruma repository.

We need to prepare onig for static linking though, so add `-fPIC` to the `CFLAGS`. If your Mono version is 32bit, make sure to add `-m32` to the `CFLAGS` too. (You may need to install a package like `gcc-multilib` to make the build work with `-m32`.)

```
./configure "CFLAGS=-fPIC"
```

Copy .libs/libonig.a to the onigwrap folder:
```
cp src/.libs/libonig.a ../TextMateSharp/onigwrap/src
cp src/oniguruma.h ../TextMateSharp/onigwrap/src
```

Build onigwrap:

```
gcc -shared -fPIC onigwrap.c libonig.a -o libonigwrap.so
```

Copy `libonigwrap.so` alongside your binary:
```
cp libonigwrap.so ../../src/TextMateSharp/Internal/Oniguruma/Native/linux/
```

Web Assembly
-----

In order to update web assembly native assets, you need to do the following:

1. Build [oniguruma](https://github.com/kkos/oniguruma) using [emscripten](https://emscripten.org/) compiler:

```
make distclean
autoreconf -vfi
source ../emsdk/emsdk_env.sh
emconfigure ./configure
emmake make
```

Then:
- Replace the existing (libonig.a)[https://github.com/danipen/TextMateSharp/blob/master/src/TextMateSharp.Wasm/libonig.a] with the generated one (it usually is generated in `<oniguruma-root>/src/.libs/` folder).
- Update the header file (oniguruma.h)[https://github.com/danipen/TextMateSharp/blob/master/onigwrap/src/oniguruma.h] with the (original one)[https://github.com/kkos/oniguruma/blob/master/src/oniguruma.h]:

```
cp src/.libs/libonig.a ../TextMateSharp/src/TextMateSharp.Wasm/
cp src/oniguruma.h ../TextMateSharp/onigwrap/src
```
