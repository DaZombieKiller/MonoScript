workspace "jay"
    configurations{"Debug", "Release"}

project "jay"
    kind "ConsoleApp"
    language "C"
    targetdir "mcs/jay"
    defines{"SKEL_DIRECTORY=\"\""}
    
    files
    {
        "mcs/jay/closure.c",
        "mcs/jay/error.c",
        "mcs/jay/lalr.c",
        "mcs/jay/lr0.c",
        "mcs/jay/main.c",
        "mcs/jay/mkpar.c",
        "mcs/jay/output.c",
        "mcs/jay/reader.c",
        "mcs/jay/symtab.c",
        "mcs/jay/verbose.c",
        "mcs/jay/warshall.c",
    }
    
    filter "configurations:Debug"
        symbols "On"
    
    filter "configurations:Release"
        optimize "On"
