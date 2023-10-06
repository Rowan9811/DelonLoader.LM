#include "Logger.h"
#include "../Assertion.h"
#include "Debug.h"
#include "../../Managers/Game.h"
#include "../../Core.h"
#include "../../Managers/AndroidData.h"
#include <stdio.h>

#ifdef _WIN32
#include <direct.h>
#elif defined(__ANDROID__)
#include <android/log.h>
#endif

#include <sstream>

#include <list>
#include <iostream>
#include <shared_mutex>
#include <filesystem>
#include <vector>

namespace fs = std::filesystem;

const std::string Logger::FilePrefix = "MelonLoader_";
const std::string Logger::FileExtension = ".log";
const std::string Logger::LatestLogFileName = "Latest";
int Logger::MaxLogs = 10;
int Logger::MaxWarnings = 100;
int Logger::MaxErrors = 100;
int Logger::WarningCount = 0;
int Logger::ErrorCount = 0;
std::mutex Logger::logMutex;
Logger::FileStream Logger::LogFile;

bool Logger::Initialize()
{
	/*if (Debug::Enabled)
	{
		MaxLogs = 0;
		MaxWarnings = 0;
		MaxErrors = 0;
	}*/

#ifdef __ANDROID__
    std::string timeStamp = GetTimestamp("%y-%m-%d_%OH-%OM-%OS");
    std::string baseFilePath = CleanAndGetFile();
    LogFile.coss = std::ofstream(baseFilePath + "/logs/" + FilePrefix + timeStamp + FileExtension);
    LogFile.latest = std::ofstream(baseFilePath + "/" + LatestLogFileName + FileExtension);
#else
    std::string logFolderPath = std::string(Core::BasePath) + "\\MelonLoader\\Logs";
	if (Core::DirectoryExists(logFolderPath.c_str()))
		CleanOldLogs(logFolderPath.c_str());
	else if (_mkdir(logFolderPath.c_str()) != 0)
	{
		Assertion::ThrowInternalFailure("Failed to Create Logs Folder!");
		return false;
	}
	std::chrono::system_clock::time_point now;
	std::chrono::milliseconds ms;
	std::tm bt;
	Core::GetLocalTime(&now, &ms, &bt);
	std::stringstream filepath;
	filepath << logFolderPath << "\\" << FilePrefix << std::put_time(&bt, "%y-%m-%d_%H-%M-%S") << "." << std::setfill('0') << std::setw(3) << ms.count() << FileExtension;
	LogFile.coss = std::ofstream(filepath.str());
	std::string latest_path = (std::string(Core::BasePath) + "\\MelonLoader\\" + LatestLogFileName + FileExtension);
	if (Core::FileExists(latest_path.c_str()))
		std::remove(latest_path.c_str());
	LogFile.latest = std::ofstream(latest_path.c_str());
#endif

	return true;
}

std::string Logger::CleanAndGetFile() {
    std::string logFolderPath = Encoding::DirectoryConcat(AndroidData::DataDir, "melonloader/etc/logs");


    if (!std::filesystem::exists(logFolderPath)) {
        if (!std::filesystem::create_directory(logFolderPath))
            Assertion::ThrowInternalFailure("Failed to Create Logs Folder!");
    } else
        CleanOldLogs(logFolderPath);

    std::string melonloaderPath = logFolderPath.substr(0, logFolderPath.find_last_of('/')) + "/" + LatestLogFileName + FileExtension;
    if (std::filesystem::exists(melonloaderPath))
        std::filesystem::remove(melonloaderPath);

    return logFolderPath.substr(0, logFolderPath.find_last_of('/'));
}

void Logger::CleanOldLogs(const std::string& logFolderPath) {
    try {
        if (MaxLogs <= 0)
            return;

        std::vector<std::string> logFiles;
        for (const auto& entry : std::filesystem::directory_iterator(logFolderPath)) {
            if (!entry.is_regular_file())
                continue;
            std::string fileName = entry.path().filename().string();
            if (fileName.find(FilePrefix) == 0 && fileName.find(FileExtension) == fileName.size() - FileExtension.size())
                logFiles.push_back(entry.path());
        }
        if (logFiles.size() < MaxLogs)
            return;
        std::sort(logFiles.begin(), logFiles.end(), [](const std::string& a, const std::string& b) {
            return std::filesystem::last_write_time(a) < std::filesystem::last_write_time(b);
        });
        int logsToDelete = logFiles.size() - MaxLogs;
        for (int i = 0; i < logsToDelete; i++) {
            std::filesystem::remove(logFiles[i]);
        }
    } catch (const std::exception& e) {
        std::cerr << "Failed to clean log folder!\n";
        std::cerr << e.what() << '\n';
    }
}

std::string Logger::GetTimestamp(std::string format)
{
    std::chrono::system_clock::time_point now;
    std::chrono::milliseconds ms;
    std::tm bt;
    Core::GetLocalTime(&now, &ms, &bt);
    std::stringstream timeStamp;
    timeStamp << std::put_time(&bt, format.c_str()) << "." << std::setfill('0') << std::setw(3) << ms.count();
    return timeStamp.str();
}

void Logger::LogToConsoleAndFile(Log log)
{
    std::lock_guard guard(logMutex);
#if __ANDROID__
    log.BuildConsoleString();
#else
    log.BuildConsoleString(std::cout);
#endif
    LogFile << log.BuildLogString();
    WriteSpacer();
}

void Logger::WriteSpacer()
{
    LogFile << std::endl;
    std::cout << std::endl;
}

void Logger::Internal_PrintModName(Console::Color meloncolor, Console::Color authorcolor, const char* name, const char* author, const char* version, const char* id)
{
    // Not using log object for this as we're modifying conventional coloring
    std::string timestamp = GetTimestamp();
    LogFile << "[" << timestamp << "] " << name << " v" << version;

    std::cout
            << Console::ColorToAnsi(Console::Color::Gray)
            << "["
            << Console::ColorToAnsi(Console::Color::Green)
            << timestamp
            << Console::ColorToAnsi(Console::Color::Gray)
            << "] "
            << Console::ColorToAnsi(meloncolor)
            << name
            << Console::ColorToAnsi(Console::Color::Gray)
            << " v"
            << version;

    if (id != NULL)
    {
        LogFile << " (" << id << ")";

        std::cout
                << " ("
                << Console::ColorToAnsi(meloncolor)
                << id
                << Console::ColorToAnsi(Console::Color::Gray)
                << ")";
    }

    LogFile << std::endl;
    std::cout
            << std::endl
            << Console::ColorToAnsi(Console::Color::Gray, false);

    if (author != NULL)
    {
        timestamp = GetTimestamp();
        LogFile << "[" << timestamp << "] by " << author << std::endl;

        std::cout
                << Console::ColorToAnsi(Console::Color::Gray)
                << "["
                << Console::ColorToAnsi(Console::Color::Green)
                << timestamp
                << Console::ColorToAnsi(Console::Color::Gray)
                << "] by "
                << Console::ColorToAnsi(authorcolor)
                << author
                << std::endl
                << Console::ColorToAnsi(Console::Color::Gray, false);
    }
}

void Logger::Internal_Msg(Console::Color meloncolor, Console::Color txtcolor, const char* namesection, const char* txt)
{
    LogToConsoleAndFile(Log(Msg, meloncolor, txtcolor, namesection, txt));
}

void Logger::Internal_Warning(const char* namesection, const char* txt)
{
    if (MaxWarnings > 0)
    {
        if (WarningCount >= MaxWarnings)
            return;
        WarningCount++;
    }
    else if (MaxWarnings < 0)
        return;

    LogToConsoleAndFile(Log(Warning, namesection, txt));
}

void Logger::Internal_Error(const char* namesection, const char* txt)
{
    if (MaxErrors > 0)
    {
        if (ErrorCount >= MaxErrors)
            return;
        ErrorCount++;
    }
    else if (MaxErrors < 0)
        return;

    LogToConsoleAndFile(Log(Error, namesection, txt));
}
