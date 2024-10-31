@echo off
cargo ndk -t arm64-v8a -o ./jniLibs build
IF "%1" NEQ "auto" (
    pause
)