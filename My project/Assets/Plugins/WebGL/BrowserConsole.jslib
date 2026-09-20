mergeInto(LibraryManager.library, {
  PlayableLog: function (messagePtr) {
    var message = UTF8ToString(messagePtr);
    console.log(message);
  }
});
