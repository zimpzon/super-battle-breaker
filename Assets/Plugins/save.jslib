mergeInto(LibraryManager.library, {
  // Your existing methods
  Save: function (json) {
    window.localStorage.setItem("super-knight-v1-savegame", UTF8ToString(json));
  },
  
    GetHostInfo: function () {
        var url = window.location.href;
        var referrer = document.referrer;
        var combined = url + "|" + referrer;
        // Unity expects a C string pointer — return the heap-allocated UTF8 string
        var lengthBytes = lengthBytesUTF8(combined) + 1;
        var stringOnWasmHeap = _malloc(lengthBytes);
        stringToUTF8(combined, stringOnWasmHeap, lengthBytes);
        return stringOnWasmHeap;
    },

    Load: function () {
    var json = window.localStorage.getItem("super-knight-v1-savegame");
    if (json === null)
        json = ''
    var bufferSize = lengthBytesUTF8(json) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(json, buffer, bufferSize);
    return buffer;
  },
  
  // New method to download a file
  DownloadFile: function (filenamePtr, contentPtr) {
    var filename = UTF8ToString(filenamePtr);
    var content = UTF8ToString(contentPtr);
    
    // Create a blob with the content
    var blob = new Blob([content], { type: 'application/json' });
    
    // Create a temporary URL for the blob
    var url = URL.createObjectURL(blob);
    
    // Create a temporary anchor element to trigger download
    var a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.style.display = 'none';
    
    // Add to DOM, click, and remove
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    
    // Clean up the URL
    URL.revokeObjectURL(url);
  },
  
  // New method to open file dialog
  OpenFileDialog: function (gameObjectNamePtr, methodNamePtr) {
    var gameObjectName = UTF8ToString(gameObjectNamePtr);
    var methodName = UTF8ToString(methodNamePtr);
    
    // Create a hidden file input
    var input = document.createElement('input');
    input.type = 'file';
    input.accept = '.json';
    input.style.display = 'none';
    
    input.onchange = function(event) {
      var file = event.target.files[0];
      if (file) {
        var reader = new FileReader();
        reader.onload = function(e) {
          var content = e.target.result;
          
          // Call Unity method with the file content
          try {
            // Use Unity's SendMessage to call the C# method
            unityInstance.SendMessage(gameObjectName, methodName, content);
          } catch (error) {
            console.error('Error calling Unity method:', error);
          }
        };
        reader.readAsText(file);
      }
      // Clean up
      document.body.removeChild(input);
    };
    
    // Add to DOM and click to open dialog
    document.body.appendChild(input);
    input.click();
  }
});