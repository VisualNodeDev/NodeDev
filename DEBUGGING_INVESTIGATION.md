# Linux Hard Debugging Crash Investigation

## Status: PARTIALLY FIXED - Still Crashes After ~25 Debug Sessions

### What Works Now
- Finalizer fix prevents immediate crashes from freeing native library on finalizer thread ✅  
- Reference counting fixes prevent crashes from mixed default/custom path instances ✅
- Race condition fix prevents concurrent initialization issues ✅  
- Server now runs 25+ debug sessions before crashing (was 3-4 before fixes)

### What Still Fails
Server crashes after approximately 20-30 debug sessions. This is reproducible with test server at `/tmp/TestBlazorDebug`.

### Observations
1. **RunWithDebug returns in 0.0 seconds** - This is suspicious. Should take 2+ seconds for Sleep node.
2. **Crash is gradual accumulation** - Not immediate, happens after multiple sessions
3. **No exceptions logged** - Server just stops responding and becomes zombie process
4. **Happens outside E2E tests** - Minimal Blazor server reproduces the issue

### Possible Remaining Issues

#### 1. Resource Leak in DebugSessionEngine
Even with proper Dispose(), there might be:
- ICorDebug COM objects not being released properly
- Managed callbacks holding references
- Event handlers not being unsubscribed

#### 2. Static State Accumulation  
The static `_globalDbgShimHandle` and `_instanceCount` might have edge cases:
- What if Dispose() throws and instance count doesn't decrement?
- What if Initialize() is called multiple times on same instance?
- Thread safety of the static variables under high concurrency?

#### 3. Native Library Reloading Issue
- Maybe we shouldn't be reusing the library handle at all?
- Try: Remove static handle reuse, load/free library for each session

#### 4. ICorDebug Process Detachment
The `Detach()` method silently catches exceptions. Maybe:
- Detach is failing but we don't know it
- Process references are accumulating
- COM object cleanup is incomplete

### Next Steps for Investigation

1. **Add comprehensive logging** to DebugSessionEngine to track:
   - Every Initialize() call with instance ID
   - Every Dispose() call with instance ID  
   - Reference count after each operation
   - Native library handle value

2. **Try removing static handle reuse**:
   - Load library fresh for each DebugSessionEngine instance
   - Free library in every Dispose()
   - See if this prevents the accumulation

3. **Check for COM object leaks**:
   - Ensure all ICorDebug interfaces are properly released
   - Check ManagedDebuggerCallbacks for reference cycles
   - Look for event handler leaks

4. **Test with actual NodeDev.Blazor.Server**:
   - User says it crashes "every single time" when running actual app
   - My test server takes 25+ runs to crash
   - There might be additional state in real app causing faster crashes

### Test Command
```bash
cd /tmp/TestBlazorDebug
dotnet run --no-build

# In another terminal:
for i in {1..50}; do 
    curl -s http://localhost:5300/test-debug
    sleep 1
done
```

### Files Modified
- `src/NodeDev.Core/Debugger/DebugSessionEngine.cs` - Finalizer, reference counting, race condition fixes
- `src/NodeDev.Core/Project.cs` - Process state handling, removed GC.Collect()
- `.gitignore` - Added TestResults/

### Commits
- 6f3eef7: Fixed finalizer threading issue
- a86bcf8: Fixed reference counting bugs  
- a8e6ad0: Fixed race condition with double-check locking

The fixes significantly improved stability but there's still a resource leak or accumulation issue that causes crashes after ~25 debug sessions.
