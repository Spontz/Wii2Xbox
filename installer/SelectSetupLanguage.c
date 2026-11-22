#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <msiquery.h>

__declspec(dllexport) UINT __stdcall SelectSetupLanguage(MSIHANDLE install)
{
    const UINT column = PRIMARYLANGID(GetUserDefaultUILanguage()) == LANG_SPANISH ? 3 : 2;
    MSIHANDLE database = MsiGetActiveDatabase(install);
    MSIHANDLE view = 0;
    MSIHANDLE format = MsiCreateRecord(0);
    MSIHANDLE row = 0;
    UINT result = ERROR_INSTALL_FAILURE;

    if (!database || !format ||
        MsiDatabaseOpenViewW(database, L"SELECT `Identifier`, `English`, `Spanish` FROM `WiiSetupText`", &view) != ERROR_SUCCESS ||
        MsiViewExecute(view, 0) != ERROR_SUCCESS)
        goto cleanup;

    while ((result = MsiViewFetch(view, &row)) == ERROR_SUCCESS)
    {
        wchar_t property[64];
        wchar_t text[2048];
        wchar_t formatted[4096];
        DWORD propertySize = ARRAYSIZE(property);
        DWORD textSize = ARRAYSIZE(text);
        DWORD formattedSize = ARRAYSIZE(formatted);

        if (MsiRecordGetStringW(row, 1, property, &propertySize) != ERROR_SUCCESS ||
            MsiRecordGetStringW(row, column, text, &textSize) != ERROR_SUCCESS ||
            MsiRecordSetStringW(format, 0, text) != ERROR_SUCCESS ||
            MsiFormatRecordW(install, format, formatted, &formattedSize) != ERROR_SUCCESS ||
            MsiSetPropertyW(install, property, formatted) != ERROR_SUCCESS)
        {
            result = ERROR_INSTALL_FAILURE;
            goto cleanup;
        }

        MsiCloseHandle(row);
        row = 0;
    }

cleanup:
    if (row) MsiCloseHandle(row);
    if (format) MsiCloseHandle(format);
    if (view) MsiCloseHandle(view);
    if (database) MsiCloseHandle(database);
    return result == ERROR_NO_MORE_ITEMS ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
}
