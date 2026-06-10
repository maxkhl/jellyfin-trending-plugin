// Injected into the Jellyfin web UI by the Trending plugin.
// 1) Adds a "Trending" entry to the left navigation drawer.
// 2) Adds a "Trending" row near the top of the home screen.
(function () {
  'use strict';

  var LINK_ID = 'trendingNavLink';
  var FAB_ID = 'trendingFab';
  var ROW_ID = 'trendingHomeRow';
  var STYLE_ID = 'trendingHomeStyles';
  var PAGE = '/Trending/Page';

  // ---------- auth + data ----------
  function getToken() {
    try {
      var raw = localStorage.getItem('jellyfin_credentials');
      if (!raw) return null;
      var creds = JSON.parse(raw);
      return (creds && creds.Servers && creds.Servers[0] && creds.Servers[0].AccessToken) || null;
    } catch (e) {
      return null;
    }
  }

  function authHeaders() {
    var t = getToken();
    return t ? { 'X-MediaBrowser-Token': t } : {};
  }

  var trendingCache = null;
  function fetchTrending() {
    if (trendingCache) return Promise.resolve(trendingCache);
    return fetch('/Trending/Items?days=30&limit=20', { headers: authHeaders() })
      .then(function (r) { return r.ok ? r.json() : []; })
      .then(function (items) { trendingCache = items || []; return trendingCache; })
      .catch(function () { return []; });
  }

  function fId(it) { return it.itemId || it.ItemId || ''; }
  function fName(it) { return it.itemName || it.ItemName || 'Unknown'; }
  function fViewers(it) {
    var v = (it.uniqueViewers != null) ? it.uniqueViewers : it.UniqueViewers;
    return v || 0;
  }

  function posterUrl(id) {
    var t = getToken();
    var u = '/Items/' + encodeURIComponent(id) + '/Images/Primary?maxWidth=280&quality=90';
    return t ? u + '&api_key=' + encodeURIComponent(t) : u;
  }

  // ---------- left drawer link ----------
  function go(e) { if (e) e.preventDefault(); window.location.href = PAGE; }

  function makeDrawerLink() {
    var a = document.createElement('a');
    a.id = LINK_ID;
    a.setAttribute('is', 'emby-linkbutton');
    a.className = 'navMenuOption emby-button';
    a.href = PAGE;
    a.addEventListener('click', go);
    a.innerHTML =
      '<span class="material-icons navMenuOptionIcon" aria-hidden="true">local_fire_department</span>' +
      '<span class="navMenuOptionText">Trending</span>';
    return a;
  }

  function tryDrawer() {
    if (document.getElementById(LINK_ID)) return true;
    var list = document.querySelector('.mainDrawer-scrollContainer') ||
               document.querySelector('.mainDrawer .navMenuOptions');
    if (!list) return false;

    var link = makeDrawerLink();
    var anchor = list.querySelector('.customMenuOptions') ||
                 list.querySelector('a.navMenuOption');
    if (anchor && anchor.parentNode === list) {
      anchor.insertAdjacentElement('afterend', link);
    } else {
      list.appendChild(link);
    }
    return true;
  }

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

  // ---------- home screen row ----------
  function injectStyles() {
    if (document.getElementById(STYLE_ID)) return;
    var s = document.createElement('style');
    s.id = STYLE_ID;
    s.textContent = [
      '#' + ROW_ID + ' .trendingScroller{display:flex;gap:12px;overflow-x:auto;overflow-y:hidden;padding:6px 3.3% 14px;scroll-behavior:smooth;}',
      '#' + ROW_ID + ' .trendingScroller::-webkit-scrollbar{height:6px;}',
      '#' + ROW_ID + ' .trendingScroller::-webkit-scrollbar-thumb{background:rgba(255,255,255,.18);border-radius:3px;}',
      '#' + ROW_ID + ' .trendingCard{flex:0 0 auto;width:140px;text-decoration:none;color:inherit;}',
      '#' + ROW_ID + ' .trendingPoster{position:relative;width:100%;aspect-ratio:2/3;border-radius:8px;background-size:cover;background-position:center;background-color:#222;transition:transform .15s;}',
      '#' + ROW_ID + ' .trendingCard:hover .trendingPoster{transform:scale(1.04);}',
      '#' + ROW_ID + ' .trendingRank{position:absolute;top:6px;left:6px;background:rgba(0,0,0,.72);color:#00a4dc;font-size:.72rem;font-weight:700;padding:1px 6px;border-radius:9px;}',
      '#' + ROW_ID + ' .trendingCardTitle{font-size:.86rem;margin-top:6px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;}',
      '#' + ROW_ID + ' .trendingCardSub{font-size:.76rem;opacity:.6;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;}'
    ].join('');
    document.head.appendChild(s);
  }

  function buildRow(items) {
    var section = document.createElement('div');
    section.id = ROW_ID;
    section.className = 'verticalSection';

    var titleC = document.createElement('div');
    titleC.className = 'sectionTitleContainer sectionTitleContainer-cards padded-left';
    var h2 = document.createElement('h2');
    h2.className = 'sectionTitle sectionTitle-cards';
    h2.textContent = '🔥 Trending';
    titleC.appendChild(h2);
    section.appendChild(titleC);

    var scroller = document.createElement('div');
    scroller.className = 'trendingScroller';

    items.forEach(function (it, i) {
      var id = fId(it);
      if (!id) return;
      var card = document.createElement('a');
      card.className = 'trendingCard';
      card.href = '#/details?id=' + encodeURIComponent(id);
      card.innerHTML =
        '<div class="trendingPoster" style="background-image:url(\'' + posterUrl(id) + '\')">' +
        '<span class="trendingRank">#' + (i + 1) + '</span></div>' +
        '<div class="trendingCardTitle"></div>' +
        '<div class="trendingCardSub"></div>';
      card.querySelector('.trendingCardTitle').textContent = fName(it);
      var v = fViewers(it);
      card.querySelector('.trendingCardSub').textContent = v === 1 ? '1 viewer' : v + ' viewers';
      scroller.appendChild(card);
    });

    section.appendChild(scroller);
    return section;
  }

  function isHome() {
    var h = (window.location.hash || '').toLowerCase().replace('#!', '#');
    return h === '' || h === '#/' || h.indexOf('#/home') === 0;
  }

  function findHomeContainer() {
    // The home page keeps its sections in a container; pick the visible one.
    var candidates = document.querySelectorAll('.homeSectionsContainer, .sections');
    for (var i = 0; i < candidates.length; i++) {
      if (candidates[i].offsetParent !== null) return candidates[i];
    }
    var vs = document.querySelector('.verticalSection');
    return vs ? vs.parentElement : null;
  }

  var rowBuilding = false;
  function ensureHomeRow() {
    if (!isHome()) return;
    var container = findHomeContainer();
    if (!container || container.querySelector('#' + ROW_ID) || rowBuilding) return;

    rowBuilding = true;
    fetchTrending().then(function (items) {
      rowBuilding = false;
      if (!items || !items.length || !isHome()) return;
      var c = findHomeContainer();
      if (!c || c.querySelector('#' + ROW_ID)) return;

      injectStyles();
      var row = buildRow(items);
      // Insert right after the first existing section (e.g. after Continue Watching).
      var first = c.querySelector('.verticalSection');
      if (first && first.nextSibling) {
        c.insertBefore(row, first.nextSibling);
      } else if (first) {
        c.appendChild(row);
      } else {
        c.insertBefore(row, c.firstChild);
      }
    }).catch(function () { rowBuilding = false; });
  }

  // ---------- drive everything ----------
  var pending = false;
  function schedule() {
    if (pending) return;
    pending = true;
    requestAnimationFrame(function () {
      pending = false;
      tryDrawer();
      ensureHomeRow();
    });
  }

  var obs = new MutationObserver(schedule);
  obs.observe(document.documentElement, { childList: true, subtree: true });
  window.addEventListener('hashchange', function () { setTimeout(ensureHomeRow, 60); });

  // Drawer: keep trying briefly, then fall back to a floating button.
  var attempts = 0;
  var timer = setInterval(function () {
    attempts++;
    if (tryDrawer()) { clearInterval(timer); return; }
    if (attempts >= 15) { clearInterval(timer); addFab(); }
  }, 1000);

  tryDrawer();
  ensureHomeRow();
})();
