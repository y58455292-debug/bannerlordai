"""Query Restart Manager users of one file; never shuts down processes or alters it."""
import ctypes as c
from ctypes import wintypes as w
import json
import sys
class UniqueProcess(c.Structure):
    _fields_ = [("pid", w.DWORD), ("start", w.FILETIME)]
class ProcessInfo(c.Structure):
    _fields_ = [("process", UniqueProcess), ("app", w.WCHAR * 256),
                ("service", w.WCHAR * 64), ("kind", c.c_int),
                ("status", w.ULONG), ("session", w.DWORD), ("restartable", w.BOOL)]
def query(path):
    rm = c.WinDLL("Rstrtmgr.dll")
    rm.RmStartSession.argtypes = [c.POINTER(w.DWORD), w.DWORD, w.LPWSTR]
    rm.RmRegisterResources.argtypes = [w.DWORD, w.UINT, c.POINTER(w.LPCWSTR),
        w.UINT, c.POINTER(UniqueProcess), w.UINT, c.POINTER(w.LPCWSTR)]
    rm.RmGetList.argtypes = [w.DWORD, c.POINTER(w.UINT), c.POINTER(w.UINT),
                           c.POINTER(ProcessInfo), c.POINTER(w.DWORD)]
    rm.RmEndSession.argtypes = [w.DWORD]
    h, key = w.DWORD(), c.create_unicode_buffer(33)
    result = {"path": path, "processes": [], "scope": "RM-listed users only; no shutdown"}
    code = rm.RmStartSession(c.byref(h), 0, key)
    if code:
        return dict(result, error_stage="start", error_code=code)
    try:
        files = (w.LPCWSTR * 1)(path)
        code = rm.RmRegisterResources(h, 1, files, 0, None, 0, None)
        if code:
            return dict(result, error_stage="register", error_code=code)
        needed, count, reasons = w.UINT(), w.UINT(), w.DWORD()
        code = rm.RmGetList(h, c.byref(needed), c.byref(count), None, c.byref(reasons))
        if code == 234:
            count.value = needed.value
            buf = (ProcessInfo * count.value)()
            code = rm.RmGetList(h, c.byref(needed), c.byref(count), buf, c.byref(reasons))
            if code == 0:
                result["processes"] = [{"pid": x.process.pid, "application": x.app,
                    "type": x.kind, "service": x.service} for x in buf[:count.value]]
        result["result_code"] = code
        return result
    finally:
        rm.RmEndSession(h)
if __name__ == "__main__":
    print(json.dumps(query(sys.argv[1]), indent=2))
