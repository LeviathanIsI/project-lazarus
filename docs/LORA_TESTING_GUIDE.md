# LoRA Adapter Testing Guide

## How to Verify Your LoRA Adapter is Actually Being Used

### 1. Visual Indicators in the UI

**Look for these status messages:**
- ✅ **"Loaded — LoRA applied (1)"** - This means llama.cpp has successfully loaded your LoRA
- ⚠️ **"LoRA LOADED but NOT APPLIED"** - The path was passed but llama.cpp didn't apply it
- ❌ **"LoRA INACTIVE"** - The LoRA isn't loaded at all

### 2. Using the Verify Button

1. Select your LoRA adapter in the dropdown
2. Click "Load" to load it
3. Click "Verify" to check the status

**What the Verify button checks:**
- Whether the runner is actually running
- Whether the LoRA path matches what you selected
- Whether llama.cpp reports any LoRA adapters as applied (from `status.LorasApplied`)

### 3. Check the Logs

The system logs LoRA-related activity. Look for these patterns:

**In the application logs:**
```
LoRA selected: C:\Users\Josh\AppData\Local\Lazarus\Models\LoRA-Adapters\Qwen3-Coder-30B-A3B-Instruct\adapter
Reloading runner to apply LoRA...
Found GGUF adapter in directory: C:\Users\Josh\AppData\Local\Lazarus\Models\LoRA-Adapters\Qwen3-Coder-30B-A3B-Instruct\adapter\adapter.gguf
Runner after reload: model=... loras=1
```

**In llama.cpp output (stderr/stdout logs):**
Look for lines containing:
- "lora"
- "adapter"
- "applying"
- ".gguf"
- "scale"

The system automatically captures these as "LoRA evidence" and counts unique .gguf files mentioned.

### 4. Test with Prompts

The most definitive test is to compare outputs with and without the LoRA:

#### Test A: Without LoRA
1. Unload the LoRA (click "Unload" button)
2. Send a test prompt that should trigger your LoRA's specialized training
3. Save the response

#### Test B: With LoRA
1. Load your LoRA adapter
2. Verify it shows "Loaded — LoRA applied (1)"
3. Send the EXACT SAME prompt
4. Compare the response

**What to look for:**
- **Different writing style** - LoRAs often change tone/voice
- **Specialized knowledge** - LoRAs add domain-specific knowledge
- **Consistent patterns** - LoRAs tend to have signature phrases or structures
- **Response quality** - Fine-tuned LoRAs often give more focused responses

### 5. Example Test Prompts

For a coding-focused LoRA like Qwen3-Coder, try:

```
# Generic test
"Write a function to reverse a string"

# More specific test
"Explain the Observer pattern in C#"

# LoRA-specific test (if you know what it was trained on)
"Write a WPF MVVM ViewModel for user settings"
```

### 6. Common Issues and Solutions

**Issue: "No GGUF files found in LoRA directory"**
- Your LoRA needs to be converted to GGUF format
- Use llama.cpp's conversion tools: `convert-lora-to-gguf.py`

**Issue: LoRA loads but doesn't affect output**
- Check the LoRA scale (Influence slider) - try 0.5 to 1.0
- Ensure the LoRA is compatible with your base model
- Check if the LoRA file is corrupted

**Issue: "LoRA applied (0)" or no count shown**
- llama.cpp couldn't parse/load the LoRA file
- Check the llama.cpp error logs for details
- Verify the GGUF file is valid

### 7. Quick Verification Checklist

- [ ] LoRA dropdown shows your adapter
- [ ] Clicking "Load" changes status to "Loaded"
- [ ] "Verify" button shows "✅ LoRA VERIFIED"
- [ ] Runner status shows "LoRA applied (1)" or higher
- [ ] Test prompts show different responses vs base model
- [ ] Logs show LoRA file path being passed to llama.cpp

### 8. Advanced Debugging

If you need to see exactly what's being passed to llama.cpp:

1. Check `LastArgs` in the runner status API
2. Look for `--lora "path/to/adapter.gguf"` in the command line
3. Check `--lora-scale` value if using custom influence

The orchestrator logs the full command at startup:
```
Starting llama-server: exe=... args=--host 127.0.0.1 --port 11434 --model "..." --lora "..."
```

## Summary

A properly loaded LoRA will:
1. Show "Loaded — LoRA applied (1)" in the UI
2. Pass verification checks
3. Produce different outputs than the base model
4. Leave evidence in the logs

If all these check out, your LoRA is working correctly!