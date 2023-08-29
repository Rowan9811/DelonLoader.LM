#include "AssemblyLoader.h"

#include <unistd.h>
#include <fcntl.h>
#include <sys/stat.h>
#include <sys/sendfile.h>
#include <dlfcn.h>

#include <iostream>
#include <filesystem>
#include <dirent.h>

#include "../Managers/AndroidData.h"
#include "./Console/Logger.h"

std::string AssemblyLoader::AssemblyTempPath;

bool AssemblyLoader::Initialize() {
    std::string modTempPath = "/data/data/";
    modTempPath += std::string(AndroidData::AppName);
    modTempPath += "/";

    AssemblyTempPath = modTempPath;

    Logger::QuickLogf(LogType::Debug, "AssemblyLoader Temp: %s", AssemblyTempPath.c_str());

    // copied from
    // https://github.com/sc2ad/QuestLoader/blob/master/libmodloader/src/modloader.cpp#L495
    struct dirent* dp;
    DIR* dir = opendir(AssemblyTempPath.c_str());
    if (dir == nullptr) {
        Logger::QuickLogf(LogType::Error, "Could not clear temp dir %s: null dir! errno: %i, msg: %s!", modTempPath.c_str(), errno, strerror(errno));
    } else {
        while ((dp = readdir(dir)) != NULL) {
            if (strlen(dp->d_name) > 3 && !strcmp(dp->d_name + strlen(dp->d_name) - 3, ".so")) {
                auto str = modTempPath + dp->d_name;
                // Delete all .so files in our modTempPath
                if (unlink(str.c_str())) {
                    Logger::QuickLogf(LogType::Warning, "Failed to delete: %s errno: %i, msg: %s", str.c_str(), errno, strerror(errno));
                } else {
                    Logger::QuickLogf(LogType::Debug, "Deleted: %s", str.c_str());
                }
            }
        }
        closedir(dir);
    }

    return true;
}

void* AssemblyLoader::Open(char* libraryPath)
{
    std::string full_path(libraryPath);

    // This is terrible.
    if (full_path.find('/') != std::string::npos) {
        const char* filename = strrchr(libraryPath, '/');
        filename++; // move past /

        // copied from
        // https://github.com/sc2ad/QuestLoader/blob/master/libmodloader/src/modloader.cpp#L252
        int infile = open(full_path.c_str(), O_RDONLY);
        off_t filesize = lseek(infile, 0, SEEK_END);
        lseek(infile, 0, SEEK_SET);

        std::string temp_path = AssemblyTempPath + filename;

        int outfile = open(temp_path.c_str(), O_CREAT | O_WRONLY, 0777);
        sendfile(outfile, infile, 0, filesize);
        close(infile);
        close(outfile);
        chmod(temp_path.c_str(), S_IRUSR | S_IWUSR | S_IXUSR | S_IRGRP | S_IWGRP | S_IXGRP);

        return dlopen(temp_path.c_str(), RTLD_NOW | RTLD_GLOBAL);
    }
    else
        return dlopen(libraryPath, RTLD_NOW | RTLD_GLOBAL);
}