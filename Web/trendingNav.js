// Injected into the Jellyfin web UI by the Trending plugin.
// Adds a "Trending" entry to the left navigation drawer, with a floating
// button fallback if the drawer markup can't be found.
(function () {
  'use strict';

  var LINK_ID = 'trendingNavLink';
  var FAB_ID = 'trendingFab';
  var TARGET = '/Trending/Page';

  function go(e) {
    if (e) e.preventDefault();
    window.location.href = TARGET;
  }

  function makeDrawerLink() {
    var a = document.createElement('a');
    a.id = LINK_ID;
    a.setAttribute('is', 'emby-linkbutton');
    a.className = 'navMenuOption emby-button';
    a.href = TARGET;
    a.addEventListener('click', go);
    a.innerHTML =
      '<span class="material-icons navMenuOptionIcon" aria-hidden="true">local_fire_department</span>' +
      '<span class="navMenuOptionText">Trending</span>';
    return a;
  }

  // Try to add the link into the left drawer. Returns true once present.
  function tryDrawer() {
    if (document.getElementById(LINK_ID)) return true;
    var list = document.querySelector('.mainDrawer-scrollContainer') ||
               document.querySelector('.mainDrawer .navMenuOptions');
    if (!list) return false;

    var link = makeDrawerLink();
    // Place it right after the Home link / custom menu options, near the top.
    var anchor = list.querySelector('.customMenuOptions') ||
                 list.querySelector('a.navMenuOption');
    if (anchor && anchor.parentNode === list) {
      anchor.insertAdjacentElement('afterend', link);
    } else {
      list.appendChild(link);
    }
    return true;
  }

  // Last-resort: a small floating button so the page is always reachable.
  function addFab() {
    if (document.getElementById(FAB_ID)) return;
    var b = document.createElement('button');
    b.id = FAB_ID;
    b.type = 'button';
    b.title = 'Trending';
    b.textContent = '🔥';
    b.addEventListener('click', go);
    b.setAttribute('style', [
      'position:fixed', 'right:18px', 'bottom:18px', 'z-index:9999',
      'width:48px', 'height:48px', 'border-radius:50%', 'border:none',
      'cursor:pointer', 'font-size:22px', 'background:#00a4dc', 'color:#fff',
      'box-shadow:0 2px 10px rgba(0,0,0,.5)'
    ].join(';'));
    document.body.appendChild(b);
  }

  // The web client is a SPA and rebuilds the drawer, so keep watching.
  var obs = new MutationObserver(function () { tryDrawer(); });
  obs.observe(document.documentElement, { childList: true, subtree: true });

  var attempts = 0;
  var timer = setInterval(function () {
    attempts++;
    if (tryDrawer()) { clearInterval(timer); return; }
    if (attempts >= 15) { clearInterval(timer); addFab(); }
  }, 1000);

  tryDrawer();
})();
