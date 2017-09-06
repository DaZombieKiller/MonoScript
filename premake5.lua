workspace "jay"
    configurations{"Debug", "Release"}

project "jay"
    kind "ConsoleApp"
    language "C"
    targetdir "jay"
    defines{"SKEL_DIRECTORY=\"\""}
    
    files
    {
        "jay/closure.c",
        "jay/error.c",
        "jay/lalr.c",
        "jay/lr0.c",
        "jay/main.c",
        "jay/mkpar.c",
        "jay/output.c",
        "jay/reader.c",
        "jay/symtab.c",
        "jay/verbose.c",
        "jay/warshall.c",
    }
    
    filter "configurations:Debug"
        symbols "On"
    
    filter "configurations:Release"
        optimize "On"
