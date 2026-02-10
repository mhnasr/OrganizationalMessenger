// Path: OrganizationalMessenger.Web/wwwroot/js/admin.js

document.addEventListener('DOMContentLoaded', function () {

    // ===== Submenu Toggle =====
    const submenuToggles = document.querySelectorAll('.menu-item.has-submenu > .menu-link');

    submenuToggles.forEach(function (toggle) {
        toggle.addEventListener('click', function (e) {
            e.preventDefault();

            const menuItem = this.parentElement;
            const isOpen = menuItem.classList.contains('open');

            // Close all other submenus (optional - remove if you want multiple open)
            // document.querySelectorAll('.menu-item.has-submenu.open').forEach(function (item) {
            //     if (item !== menuItem) {
            //         item.classList.remove('open');
            //     }
            // });

            // Toggle current submenu
            menuItem.classList.toggle('open');
        });
    });

    // ===== Sidebar Toggle (Mobile) =====
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebar = document.getElementById('sidebar');
    const sidebarOverlay = document.getElementById('sidebarOverlay');

    if (sidebarToggle && sidebar) {
        sidebarToggle.addEventListener('click', function () {
            sidebar.classList.toggle('show');
            if (sidebarOverlay) {
                sidebarOverlay.classList.toggle('show');
            }
        });
    }

    if (sidebarOverlay) {
        sidebarOverlay.addEventListener('click', function () {
            sidebar.classList.remove('show');
            this.classList.remove('show');
        });
    }

    // ===== Auto scroll to active menu item =====
    const activeMenuItem = document.querySelector('.sidebar-menu .menu-link.active');
    if (activeMenuItem) {
        setTimeout(function () {
            activeMenuItem.scrollIntoView({
                behavior: 'smooth',
                block: 'center'
            });
        }, 100);
    }

    // ===== Update Reports Badge =====
    function updateReportsBadge() {
        fetch('/Admin/Dashboard/GetPendingReportsCount')
            .then(response => response.json())
            .then(data => {
                const badge = document.getElementById('reportsBadge');
                if (badge && data.count !== undefined) {
                    badge.textContent = data.count;
                    badge.style.display = data.count > 0 ? 'inline' : 'none';
                }
            })
            .catch(err => console.log('Could not fetch reports count'));
    }

    // Update every 60 seconds
    updateReportsBadge();
    setInterval(updateReportsBadge, 60000);

});
