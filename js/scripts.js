$(function () {
  $('[data-toggle="tooltip"]').tooltip()
})

$(document).ready(function() {
  $('.image-link').magnificPopup({
    type: 'image',
    removalDelay: 500, //delay removal by X to allow out-animation
    image: {
      cursor: 'null',
    },
    callbacks: {
      beforeOpen: function() {
        // just a hack that adds mfp-anim class to markup 
         this.st.image.markup = this.st.image.markup.replace('mfp-figure', 'mfp-figure mfp-with-anim');
         this.st.mainClass = this.st.el.attr('data-effect');
      }
    },
    closeOnContentClick: true,
    midClick: true // allow opening popup on middle mouse click. Always set it to true if you don't provide alternative source.
  });
  convertToMB();
  formatChangelog();
});

function convertToMB(){
  var element1 = document.getElementById("size-text")
  var num = element1.textContent;
  var n=num.slice(6);
  var number = Number(n);
  number = number/1024;
  number = (number / 1024).toFixed(2);
  var numberString = String(number);
  element1.innerHTML = "Size: ".concat(numberString," MB");
}

function formatChangelog() {
  var element2 = document.getElementById("latestChanges")
  var log = "";
  $.get('./assets/latestChanges.txt', function(data) {
    log = data.replace(/(?:\r\n|\r|\n)/g, '<br />');
    element2.innerHTML = log;
  }, 'text');
}

function thanks() {
  setTimeout(function () {
      document.location.pathname = "WinHue3/download.html";
  }, 5000);
}
