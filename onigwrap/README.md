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

Build and configure oniguruma [following the instructions](https://github.com/kkos/oniguruma#case-2-manual-compilation-on-linux-unix-and-cygwin-platform) on the Oniguruma repository.

Copy `$libs/libonig.a` to the onigwrap folder.

Now we build onigwrap:

`clang -dynamiclib -L. -lonig -o libonigwrap.dylib onigwrap.c`

Take the onigwrap.dylib and put it alongside your binary.

Windows
-------
* Setup and install C++ Build [Tools for Visual Studio](https://visualstudio.microsoft.com/es/thank-you-downloading-visual-studio/?sku=BuildTools&rel=16).
* Open a console and setup the Visual C compiler env vars, according to your platform for example:

`"C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\VC\Auxiliary\Build\vcvars64.bat"`

Then build and configure oniguruma [following the instructions](https://github.com/kkos/oniguruma#case-3-windows-6432bit-platform-visual-studio) on the Oniguruma repository.

Copy `onig\_s.lib` and `oniguruma.h` to the `src` folder.

Build onigwrap:

`cl.exe /DONIG_EXTERN=extern /D_USRDLL /D_WINDLL onigwrap.c /link onig_s.lib /DLL /OUT:onigwrap.dll`

Copy onigwrap.dll to the folder with your binary.

Linux
-----

Build and configure oniguruma [following the instructions](https://github.com/kkos/oniguruma#case-2-manual-compilation-on-linux-unix-and-cygwin-platform) on the Oniguruma repository.

We need to prepare onig for static linking though, so add `-fPIC` to the `CFLAGS`. If your Mono version is 32bit, make sure to add `-m32` to the `CFLAGS` too. (You may need to install a package like `gcc-multilib` to make the build work with `-m32`.)

`./configure "CFLAGS=-fPIC"`

Copy .libs/libonig.a to the onigwrap folder.

Build onigwrap:

`gcc -shared -fPIC onigwrap.c libonig.a -o libonigwrap.so`

Copy `libonigwrap.so` alongside your binary.

Web Assembly
-----

In order to update web assembly native assets, you need to do the following:

1. Build [oniguruma](https://github.com/kkos/oniguruma) using [emscripten](https://emscripten.org/) compiler:

```
autoreconf -vfi
emconfigure ./configure
emmake make
```

Then:
- Replace the existing (libonig.a)[https://github.com/danipen/TextMateSharp/blob/master/src/TextMateSharp.Wasm/libonig.a] with the generated one (it usually is generated in `<oniguruma-root>/src/.libs/` folder).
- Update the header file (oniguruma.h)[https://github.com/danipen/TextMateSharp/blob/master/onigwrap/src/oniguruma.h] with the (original one)[https://github.com/kkos/oniguruma/blob/master/src/oniguruma.h].
