// Global state variables
let allItems = [];
let currentRequests = [];
let categories = [];
let spaces = [];
let isEditing = false;
let hasOverdueLoans = false;

// Check token presence before loading
const token = localStorage.getItem('session_token');
if (!token) {
    window.location.href = 'index.html';
}

// Initialization on DOM Content Loaded
document.addEventListener('DOMContentLoaded', async () => {
    await validateSession();
    await fetchMetadata();
    
    // Check if student has overdue loans before rendering catalog
    if (localStorage.getItem('user_role_id') === '2') {
        await checkOverdueStatus();
    }
    
    await fetchInventory();
});

// Validate user session token
async function validateSession() {
    try {
        const response = await fetch('/api/auth/validate', {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (!response.ok) {
            throw new Error('Sesión expirada.');
        }

        const user = await response.json();
        
        // Update user UI details
        document.getElementById('userNameLabel').textContent = user.nombre;
        document.getElementById('userRoleLabel').textContent = user.rol;

        // Check if Admin
        if (user.rolId === 1) { // Admin
            document.getElementById('addBtn').style.display = 'inline-flex';
            document.getElementById('dashboardTitle').textContent = 'Administración de Inventario';
            document.getElementById('dashboardSubtitle').textContent = 'Control y registro de equipos físicos del laboratorio.';
            
            // Show Admin-only sidebar items
            const menuRequests = document.getElementById('menu-requests');
            if(menuRequests) menuRequests.style.display = 'flex';
            
            const menuReturns = document.getElementById('menu-returns');
            if(menuReturns) menuReturns.style.display = 'flex';
            
            const menuHistory = document.getElementById('menu-history');
            if(menuHistory) menuHistory.style.display = 'flex';
            
            const menuStats = document.getElementById('menu-stats');
            if(menuStats) menuStats.style.display = 'flex';
            
            const menuCategories = document.getElementById('menu-categories');
            if(menuCategories) menuCategories.style.display = 'flex';
            
            const menuAudit = document.getElementById('menu-audit');
            if(menuAudit) menuAudit.style.display = 'flex';
            
            const menuBloqueados = document.getElementById('menu-bloqueados');
            if(menuBloqueados) menuBloqueados.style.display = 'flex';
            
            localStorage.setItem('user_role_id', '1');

            // Iniciar notificaciones para admin
            startNotificationPolling();
        } else { // Student
            document.getElementById('addBtn').style.display = 'none';
            document.getElementById('dashboardTitle').textContent = 'Catálogo de Laboratorio';
            document.getElementById('dashboardSubtitle').textContent = 'Consulta y verificación de stock disponible en tiempo real.';
            
            const menuStudentHistory = document.getElementById('menu-student-history');
            if(menuStudentHistory) menuStudentHistory.style.display = 'flex';
            
            const menuStats = document.getElementById('menu-stats');
            if(menuStats) menuStats.style.display = 'flex';
            
            localStorage.setItem('user_role_id', '2');
            
            // Iniciar notificaciones para estudiante
            startNotificationPolling();
        }
    } catch (err) {
        handleLogout();
    }
}

// UI Utilities
function showToast(message, type = 'success') {
    const toast = document.createElement('div');
    toast.className = 'glass-panel';
    const color = type === 'success' ? 'var(--color-success)' : 'var(--color-danger)';
    
    toast.style.cssText = `
        position: fixed; bottom: 30px; right: 30px; padding: 15px 25px; 
        border-left: 4px solid ${color}; z-index: 9999;
        transform: translateY(100px); opacity: 0; transition: all 0.3s ease;
        box-shadow: var(--shadow-lg); color: var(--text-primary);
        font-weight: 500; display: flex; align-items: center; gap: 10px;
    `;
    
    const icon = type === 'success' ? '✅' : '❌';
    toast.innerHTML = `<span>${icon}</span> <span>${message}</span>`;
    document.body.appendChild(toast);
    
    setTimeout(() => { toast.style.transform = 'translateY(0)'; toast.style.opacity = '1'; }, 50);
    setTimeout(() => { 
        toast.style.transform = 'translateY(100px)'; toast.style.opacity = '0';
        setTimeout(() => toast.remove(), 300);
    }, 4000);
}

function showConfirm(message) {
    return new Promise((resolve) => {
        const overlay = document.createElement('div');
        overlay.className = 'modal-overlay';
        overlay.style.display = 'flex';
        
        overlay.innerHTML = `
            <div class="modal-content glass-panel" style="max-width: 400px; text-align: center;">
                <h3 style="margin-bottom: 15px;">Confirmación</h3>
                <p style="margin-bottom: 25px; color: var(--text-secondary);">${message}</p>
                <div style="display: flex; justify-content: center; gap: 15px;">
                    <button class="btn btn-secondary" id="confirmNo">Cancelar</button>
                    <button class="btn btn-danger" id="confirmYes">Continuar</button>
                </div>
            </div>
        `;
        document.body.appendChild(overlay);
        
        document.getElementById('confirmNo').onclick = () => { overlay.remove(); resolve(false); };
        document.getElementById('confirmYes').onclick = () => { overlay.remove(); resolve(true); };
    });
}

function showPrompt(message, placeholder = "") {
    return new Promise((resolve) => {
        const overlay = document.createElement('div');
        overlay.className = 'modal-overlay';
        overlay.style.display = 'flex';
        
        overlay.innerHTML = `
            <div class="modal-content glass-panel" style="max-width: 450px;">
                <h3 style="margin-bottom: 15px;">Información Adicional</h3>
                <p style="margin-bottom: 15px; color: var(--text-secondary);">${message}</p>
                <input type="text" id="promptInput" class="form-control" placeholder="${placeholder}" style="margin-bottom: 25px;">
                <div style="display: flex; justify-content: flex-end; gap: 15px;">
                    <button class="btn btn-secondary" id="promptNo">Cancelar</button>
                    <button class="btn btn-primary" id="promptYes">Aceptar</button>
                </div>
            </div>
        `;
        document.body.appendChild(overlay);
        
        document.getElementById('promptInput').focus();
        document.getElementById('promptNo').onclick = () => { overlay.remove(); resolve(null); };
        document.getElementById('promptYes').onclick = () => { overlay.remove(); resolve(document.getElementById('promptInput').value); };
    });
}

// Notifications System
let notificationInterval = null;

function toggleNotifications() {
    const dropdown = document.getElementById('notificationsDropdown');
    dropdown.style.display = dropdown.style.display === 'none' ? 'block' : 'none';
    if (dropdown.style.display === 'block') {
        fetchNotifications();
    }
}

// Close dropdown when clicking outside
document.addEventListener('click', (e) => {
    const wrapper = document.getElementById('notificationsWrapper');
    const dropdown = document.getElementById('notificationsDropdown');
    if (wrapper && dropdown && !wrapper.contains(e.target)) {
        dropdown.style.display = 'none';
    }
});

function startNotificationPolling() {
    if (notificationInterval) clearInterval(notificationInterval);
    fetchNotifications(); // Initial fetch
    notificationInterval = setInterval(fetchNotifications, 60000); // Poll every 60s
}

async function fetchNotifications() {
    try {
        const response = await fetch('/api/notificaciones', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (response.ok) {
            const data = await response.json();
            renderNotifications(data.notificaciones, data.noLeidas);
        }
    } catch (error) {
        console.error('Error fetching notifications', error);
    }
}

function renderNotifications(notificaciones, noLeidas) {
    const badge = document.getElementById('notificationBadge');
    const list = document.getElementById('notificationsList');
    
    if (noLeidas > 0) {
        badge.textContent = noLeidas > 99 ? '99+' : noLeidas;
        badge.style.display = 'block';
    } else {
        badge.style.display = 'none';
    }
    
    list.innerHTML = '';
    
    if (!notificaciones || notificaciones.length === 0) {
        list.innerHTML = '<div class="notification-item empty">No tienes notificaciones.</div>';
        return;
    }
    
    notificaciones.forEach(notif => {
        const item = document.createElement('div');
        item.className = `notification-item ${notif.leida === 0 ? 'unread' : ''}`;
        
        const date = new Date(notif.fecha).toLocaleString();
        
        item.innerHTML = `
            <div class="notif-title">${notif.titulo}</div>
            <div class="notif-msg">${notif.mensaje}</div>
            <div class="notif-time">${date}</div>
        `;
        
        if (notif.leida === 0) {
            item.onclick = () => marcarComoLeida(notif.notificacionId, item);
        }
        
        list.appendChild(item);
    });
}

async function marcarComoLeida(id, element) {
    try {
        const response = await fetch(`/api/notificaciones/${id}/leer`, {
            method: 'POST',
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (response.ok) {
            element.classList.remove('unread');
            element.onclick = null;
            fetchNotifications(); // Update count
        }
    } catch (err) {
        console.error('Error', err);
    }
}

async function marcarTodasComoLeidas() {
    try {
        const response = await fetch('/api/notificaciones/leer-todas', {
            method: 'POST',
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (response.ok) {
            fetchNotifications(); // Refresh view
            document.getElementById('notificationsDropdown').style.display = 'none';
        }
    } catch (err) {
        console.error('Error', err);
    }
}

async function checkOverdueStatus() {
    try {
        const response = await fetch('/api/estudiante/mis-solicitudes', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (response.ok) {
            const misSolicitudes = await response.json();
            const now = new Date();
            hasOverdueLoans = misSolicitudes.some(req => {
                if (req.estado === 'APROBADO' && req.fechaDevolucion) {
                    const devDate = new Date(req.fechaDevolucion);
                    return devDate < now;
                }
                return false;
            });
        }
    } catch (error) {
        console.error('Error checking overdue status:', error);
    }
}

// Log out user
function handleLogout() {
    fetch('/api/auth/logout', {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${token}`
        }
    }).finally(() => {
        localStorage.clear();
        window.location.href = 'index.html';
    });
}

// Fetch categories and spaces dropdown metadata
async function fetchMetadata() {
    try {
        const response = await fetch('/api/inventario/metadata', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (response.ok) {
            const data = await response.json();
            categories = data.categories || [];
            spaces = data.spaces || [];

            // Populate category filter
            const filterSelect = document.getElementById('categoryFilter');
            const formCategorySelect = document.getElementById('itemCategory');
            
            categories.forEach(cat => {
                // Filter dropdown
                const optFilter = document.createElement('option');
                optFilter.value = cat.categoriaId;
                optFilter.textContent = cat.nombre;
                filterSelect.appendChild(optFilter);

                // Modal Form dropdown
                const optForm = document.createElement('option');
                optForm.value = cat.categoriaId;
                optForm.textContent = cat.nombre;
                formCategorySelect.appendChild(optForm);
            });

            // Populate spaces dropdown in Modal Form
            const formSpaceSelect = document.getElementById('itemSpace');
            spaces.forEach(sp => {
                const opt = document.createElement('option');
                opt.value = sp.espacioId;
                opt.textContent = `${sp.nombre} (${sp.taller?.nombre || 'Taller'})`;
                formSpaceSelect.appendChild(opt);
            });
        }
    } catch (error) {
        console.error('Error fetching metadata:', error);
    }
}

// Fetch inventory list
async function fetchInventory() {
    try {
        const isAdmin = localStorage.getItem('user_role_id') === '1';
        const url = isAdmin ? '/api/inventario' : '/api/estudiante/catalogo';
        const response = await fetch(url, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (response.ok) {
            allItems = await response.json();
            renderItems(allItems);
        }
    } catch (error) {
        console.error('Error fetching inventory:', error);
    }
}

// Utility to remove image from form
function removeImage(inputId, hiddenId, previewContainerId) {
    document.getElementById(inputId).value = '';
    document.getElementById(hiddenId).value = '';
    document.getElementById(previewContainerId).style.display = 'none';
}

// Render list cards in the DOM
function renderItems(items) {
    const grid = document.getElementById('inventoryGrid');
    const emptyState = document.getElementById('emptyState');
    grid.innerHTML = '';

    if (items.length === 0) {
        emptyState.style.display = 'block';
    } else {
        emptyState.style.display = 'none';
    }

    const isAdmin = localStorage.getItem('user_role_id') === '1';
    let lowStockCount = 0;

    // Control widget display
    const widget = document.getElementById('lowStockWidget');
    if (widget) {
        widget.style.display = 'none';
    }

    items.forEach(item => {
        const card = document.createElement('div');
        
        if (isAdmin) {
            // Admin View (ItemInventario)
            card.className = `item-card glass-panel ${item.estadoOperativo.toLowerCase()}`;
            
            let statusClass = 'status-disponible';
            if (item.estadoPrestamo === 'PRESTADO') statusClass = 'status-prestado';
            if (item.estadoOperativo === 'DADO_DE_BAJA') statusClass = 'status-baja';

            if (item.stock <= (item.stockMinimo || 1)) {
                lowStockCount++;
            }

            let adminButtons = `
                <div class="card-actions">
                    <button onclick="agregarStock(${item.itemId})" class="btn-icon" title="Agregar Stock" style="color: var(--color-success)">➕</button>
                    ${item.stockDefectuoso > 0 ? `<button onclick="abrirModalReparar(${item.itemId}, ${item.stockDefectuoso})" class="btn-icon" title="Reparar Defectuosos" style="color: var(--color-warning)">🛠️</button>` : ''}
                    <button onclick="openEditModal(${item.itemId})" class="btn-icon" title="Editar">✏️</button>
                    <button onclick="deleteItem(${item.itemId})" class="btn-icon" title="Eliminar" style="hover: border-color: var(--color-danger)">🗑️</button>
                </div>
            `;

            card.innerHTML = `
                <div>
                    <div class="card-header">
                        <span class="item-code">
                            ${item.codigoActivo} 
                            ${item.esPublico === 0 ? '<span style="background: var(--color-danger); color: white; padding: 2px 6px; border-radius: 4px; font-size: 0.7rem; margin-left: 5px;">Oculto</span>' : ''}
                        </span>
                        <div style="display: flex; gap: 5px; flex-direction: column; align-items: flex-end;">
                            <span class="item-status ${item.stock <= (item.stockMinimo || 1) ? 'status-baja' : 'status-disponible'}" ${item.stock <= (item.stockMinimo || 1) ? 'style="background: rgba(244, 67, 54, 0.1); color: var(--color-danger);"' : ''}>Stock: ${item.stock}</span>
                            ${item.stockDefectuoso > 0 ? `<span class="item-status" style="background: rgba(244, 67, 54, 0.1); color: var(--color-danger); border-color: rgba(244, 67, 54, 0.2); font-size: 0.65rem;">Defectuosos: ${item.stockDefectuoso}</span>` : ''}
                        </div>
                    </div>
                    <h3 class="item-name">${item.nombre}</h3>
                    <p class="item-desc">${item.descripcion || 'Sin descripción técnica disponible.'}</p>
                    ${item.imagenUrl ? `<div style="text-align: center; margin-top: 10px;"><img src="${item.imagenUrl}" style="max-height: 120px; border-radius: 8px;"></div>` : ''}
                </div>
                
                <div>
                    <div class="item-meta">
                        <div class="meta-field">Ubicación: <span>${item.espacio?.nombre || 'Laboratorio'}</span></div>
                        <div class="meta-field">Categoría: <span>${item.categoria?.nombre || 'General'}</span></div>
                    </div>
                    <div class="item-meta" style="border-top: none; padding-top: 4px;">
                        <div class="meta-field">Marca/Modelo: <span>${item.marca || 'N/A'} - ${item.modelo || 'N/A'}</span></div>
                    </div>
                    ${adminButtons}
                </div>
            `;
        } else if (isDocente) {
            card.className = `item-card glass-panel`;
            card.innerHTML = `
                <div>
                    <div class="card-header">
                        <span class="item-code">${item.codigoActivo || 'S/M'}</span>
                        <span class="item-status status-disponible">Disponibles: ${item.stock}</span>
                    </div>
                    <h3 class="item-name">${item.nombre}</h3>
                    <p class="item-desc">${item.descripcion || 'Sin descripción disponible.'}</p>
                    ${item.imagenUrl ? `<div style="text-align: center; margin-top: 10px;"><img src="${item.imagenUrl}" style="max-height: 120px; border-radius: 8px;"></div>` : ''}
                </div>
                <div>
                    <button onclick="openItemInfoModal(${item.itemId})" class="btn btn-secondary" style="width: 100%; margin-top: 15px;">
                        Más información
                    </button>
                </div>
            `;
        } else {
            // Student View (CatalogoItemDto)
            card.className = `item-card glass-panel`;
            const hasStock = item.stockDisponible > 0;
            
            card.innerHTML = `
                <div>
                    <div class="card-header">
                        <span class="item-code">${item.modelo || 'S/M'}</span>
                        <span class="item-status status-disponible">Disponibles: ${item.stockDisponible}</span>
                    </div>
                    <h3 class="item-name">${item.nombre}</h3>
                    <p class="item-desc">${item.descripcion || 'Sin descripción técnica disponible.'}</p>
                    ${item.imagenUrl ? `<div style="text-align: center; margin-top: 10px;"><img src="${item.imagenUrl}" style="max-height: 120px; border-radius: 8px;"></div>` : ''}
                </div>
                <div>
                    <button onclick="openAddToCartModal('${item.nombre.replace(/'/g, "\\'")}', ${item.stockDisponible})" class="btn btn-primary" style="width: 100%; margin-top: 15px;" ${!hasStock || hasOverdueLoans ? 'disabled' : ''}>
                        ${hasOverdueLoans ? 'Bloqueado (Vencido)' : (hasStock ? 'Solicitar Préstamo' : 'Sin Stock')}
                    </button>
                </div>
            `;
        }
        
        grid.appendChild(card);
    });

    if (!isAdmin && hasOverdueLoans) {
        const warningBox = document.createElement('div');
        warningBox.className = 'glass-panel';
        warningBox.style.cssText = 'background: rgba(244, 67, 54, 0.1); color: var(--color-danger); border: 1px solid rgba(244, 67, 54, 0.2); padding: 15px; margin-bottom: 20px; grid-column: 1 / -1; font-weight: bold; text-align: center; border-radius: 8px;';
        warningBox.innerHTML = '⚠️ Tienes préstamos vencidos. La solicitud de nuevos préstamos ha sido bloqueada hasta que devuelvas los elementos correspondientes.';
        grid.prepend(warningBox);
    }

    if (isAdmin && widget) {
        document.getElementById('lowStockCount').textContent = lowStockCount;
        widget.style.display = lowStockCount > 0 ? 'flex' : 'none';
    }
}

// Search and filter inventory locally
function filterItems() {
    const query = document.getElementById('searchInput').value.toLowerCase().trim();
    const categoryVal = document.getElementById('categoryFilter').value;
    const isAdmin = localStorage.getItem('user_role_id') === '1';
    const isDocente = localStorage.getItem('user_role_id') === '3';

    const filtered = allItems.filter(item => {
        let matchesQuery = false;
        let matchesCategory = true;
        
        if (isAdmin || isDocente) {
            matchesQuery = item.nombre.toLowerCase().includes(query) || 
                           item.codigoActivo.toLowerCase().includes(query) ||
                           (item.descripcion && item.descripcion.toLowerCase().includes(query)) ||
                           (item.marca && item.marca.toLowerCase().includes(query));
            matchesCategory = categoryVal === "" || item.categoriaId.toString() === categoryVal;
        } else {
            matchesQuery = item.nombre.toLowerCase().includes(query) || 
                           (item.descripcion && item.descripcion.toLowerCase().includes(query)) ||
                           (item.modelo && item.modelo.toLowerCase().includes(query));
            // Catalog doesn't currently support category filtering easily unless DTO is updated.
        }

        return matchesQuery && matchesCategory;
    });

    renderItems(filtered);
}

// --- Cart Logic ---
let cart = [];

function openAddToCartModal(itemName, maxStock) {
    document.getElementById('addToCartItemName').value = itemName;
    document.getElementById('addToCartItemDisplayName').textContent = itemName;
    const qtyInput = document.getElementById('addToCartQuantity');
    qtyInput.max = maxStock;
    qtyInput.value = 1;
    document.getElementById('addToCartMaxStockDisplay').textContent = `Máximo disponible: ${maxStock}`;
    document.getElementById('addToCartModal').style.display = 'flex';
}

function closeAddToCartModal() {
    document.getElementById('addToCartModal').style.display = 'none';
}

function confirmAddToCart(e) {
    e.preventDefault();
    const itemName = document.getElementById('addToCartItemName').value;
    const maxStock = parseInt(document.getElementById('addToCartQuantity').max);
    const qty = parseInt(document.getElementById('addToCartQuantity').value);
    
    // check if already in cart
    const existing = cart.find(i => i.nombreItem === itemName);
    if (existing) {
        if (existing.cantidad + qty > maxStock) {
            showToast('No puedes exceder el stock máximo disponible', 'error');
            return;
        }
        existing.cantidad += qty;
    } else {
        cart.push({ nombreItem: itemName, cantidad: qty });
    }
    
    updateCartUI();
    closeAddToCartModal();
    showToast('Añadido al carrito', 'success');
}

function updateCartUI() {
    const badge = document.getElementById('cartCountBadge');
    if(badge) badge.textContent = cart.length;
    
    const floatBtn = document.getElementById('cartFloatingBtn');
    if(floatBtn) floatBtn.style.display = cart.length > 0 ? 'flex' : 'none';
    
    const cartItemsList = document.getElementById('cartItemsList');
    if(cartItemsList) {
        cartItemsList.innerHTML = '';
        cart.forEach((item, index) => {
            const div = document.createElement('div');
            div.style.display = 'flex';
            div.style.justifyContent = 'space-between';
            div.style.marginBottom = '10px';
            div.style.padding = '8px';
            div.style.background = 'rgba(255,255,255,0.05)';
            div.style.borderRadius = '6px';
            div.innerHTML = `
                <span><strong>${item.cantidad}x</strong> ${item.nombreItem}</span>
                <button type="button" onclick="removeFromCart(${index})" class="btn-icon" style="color: var(--color-danger); padding: 0;">❌</button>
            `;
            cartItemsList.appendChild(div);
        });
    }
}

function removeFromCart(index) {
    cart.splice(index, 1);
    updateCartUI();
    if(cart.length === 0) closeCartModal();
}

function openCartModal() {
    if(cart.length === 0) return;
    
    // Set date constraints
    const today = new Date();
    const minDateStr = today.toISOString().split('T')[0];
    
    // Default to +2 days
    const defaultDate = new Date();
    defaultDate.setDate(defaultDate.getDate() + 2);
    const defaultDateStr = defaultDate.toISOString().split('T')[0];
    
    const dateInput = document.getElementById('loanReturnDate');
    if (dateInput) {
        flatpickr(dateInput, {
            locale: "es",
            minDate: minDateStr,
            defaultDate: defaultDateStr,
            dateFormat: "Y-m-d",
            altInput: true,
            altFormat: "j \\de F, Y",
        });
    }
    
    document.getElementById('cartModal').style.display = 'flex';
}

function closeCartModal() {
    document.getElementById('cartModal').style.display = 'none';
}

async function submitCart(e) {
    e.preventDefault();
    if(cart.length === 0) return;
    
    const requestData = {
        Items: cart,
        FechaDevolucion: document.getElementById('loanReturnDate') ? document.getElementById('loanReturnDate').value : null
    };

    try {
        const response = await fetch('/api/estudiante/prestamo', {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestData)
        });

        if (response.ok) {
            cart = [];
            updateCartUI();
            closeCartModal();
            showToast('Préstamo solicitado exitosamente.', 'success');
            await fetchInventory();
            if (typeof fetchStudentHistory === 'function') {
                await fetchStudentHistory(); // refresh student history
            }
        } else {
            const data = await response.json();
            showToast(data.mensaje || 'Error al solicitar el préstamo.', 'error');
        }
    } catch (error) {
        console.error('Error submitting cart:', error);
        showToast('Error de red. Intenta nuevamente.', 'error');
    }
}

// Open create dialog modal
function openCreateModal() {
    isEditing = false;
    document.getElementById('modalTitle').textContent = 'Agregar Nuevo Elemento';
    document.getElementById('itemForm').reset();
    document.getElementById('itemId').value = '';
    document.getElementById('modalAlert').style.display = 'none';
    
    // Default selects
    if (categories.length > 0) document.getElementById('itemCategory').value = categories[0].categoriaId;
    if (spaces.length > 0) document.getElementById('itemSpace').value = spaces[0].espacioId;
    
    const cantidadGroup = document.getElementById('cantidadGroup');
    if (cantidadGroup) cantidadGroup.style.display = 'block';
    
    if (document.getElementById('itemStockMinimo')) {
        document.getElementById('itemStockMinimo').value = 1;
    }
    
    if (document.getElementById('itemIsPublic')) {
        document.getElementById('itemIsPublic').checked = true;
    }

    document.getElementById('itemImagePreviewContainer').style.display = 'none';

    document.getElementById('itemModal').style.display = 'flex';
}

// Open edit dialog modal populated with data
async function openEditModal(id) {
    isEditing = true;
    document.getElementById('modalTitle').textContent = 'Editar Ficha de Elemento';
    document.getElementById('modalAlert').style.display = 'none';
    
    const cantidadGroup = document.getElementById('cantidadGroup');
    if (cantidadGroup) cantidadGroup.style.display = 'block';

    try {
        const response = await fetch(`/api/inventario/${id}`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (response.ok) {
            const item = await response.json();
            document.getElementById('itemId').value = item.itemId;
            document.getElementById('itemCode').value = item.codigoActivo;
            document.getElementById('itemName').value = item.nombre;
            document.getElementById('itemBrand').value = item.marca || '';
            document.getElementById('itemModel').value = item.modelo || '';
            document.getElementById('itemSerial').value = item.numeroSerie || '';
            document.getElementById('itemCategory').value = item.categoriaId;
            document.getElementById('itemSpace').value = item.espacioId;
            document.getElementById('itemDescription').value = item.descripcion || '';
            document.getElementById('itemObservations').value = item.observaciones || '';
            if (document.getElementById('itemCantidad')) {
                document.getElementById('itemCantidad').value = item.stock || 1;
            }
            if (document.getElementById('itemStockMinimo')) {
                document.getElementById('itemStockMinimo').value = item.stockMinimo || 1;
            }
            if (document.getElementById('itemIsPublic')) {
                document.getElementById('itemIsPublic').checked = item.esPublico !== 0; // defaults to true if undefined
            }
            if (item.imagenUrl) {
                document.getElementById('itemImageUrl').value = item.imagenUrl;
                document.getElementById('itemImagePreview').src = item.imagenUrl;
                document.getElementById('itemImagePreviewContainer').style.display = 'block';
            } else {
                document.getElementById('itemImageUrl').value = '';
                document.getElementById('itemImagePreviewContainer').style.display = 'none';
            }

            document.getElementById('itemModal').style.display = 'flex';
        }
    } catch (error) {
        console.error('Error fetching item details:', error);
    }
}

// Close modal dialog
function closeItemModal() {
    document.getElementById('itemModal').style.display = 'none';
}

// Form submit action (Create & Update)
async function handleFormSubmit(e) {
    e.preventDefault();
    const modalAlert = document.getElementById('modalAlert');
    modalAlert.style.display = 'none';

    const id = document.getElementById('itemId').value;
    const bodyData = {
        codigoActivo: document.getElementById('itemCode').value.trim(),
        nombre: document.getElementById('itemName').value.trim(),
        marca: document.getElementById('itemBrand').value.trim() || null,
        modelo: document.getElementById('itemModel').value.trim() || null,
        numeroSerie: document.getElementById('itemSerial').value.trim() || null,
        categoriaId: parseInt(document.getElementById('itemCategory').value),
        espacioId: parseInt(document.getElementById('itemSpace').value),
        descripcion: document.getElementById('itemDescription').value.trim() || null,
        observaciones: document.getElementById('itemObservations').value.trim() || null,
        stock: document.getElementById('itemCantidad') ? parseInt(document.getElementById('itemCantidad').value) : 1,
        stockMinimo: document.getElementById('itemStockMinimo') ? parseInt(document.getElementById('itemStockMinimo').value) : 1,
        esPublico: document.getElementById('itemIsPublic') ? (document.getElementById('itemIsPublic').checked ? 1 : 0) : 1
    };

    const url = isEditing ? `/api/inventario/${id}` : '/api/inventario';
    const method = isEditing ? 'PUT' : 'POST';

    try {
        const fileInput = document.getElementById('itemImage');
        if (fileInput && fileInput.files.length > 0) {
            const formData = new FormData();
            formData.append('file', fileInput.files[0]);
            const uploadRes = await fetch('/api/archivos/upload/items', {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${token}` },
                body: formData
            });
            const uploadData = await uploadRes.json();
            if (!uploadRes.ok) {
                throw new Error(uploadData.mensaje || 'Error al subir la imagen.');
            }
            bodyData.imagenUrl = uploadData.url;
        }

        const response = await fetch(url, {
            method: method,
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(bodyData)
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.mensaje || 'Error al guardar el elemento.');
        }

        closeItemModal();
        await fetchInventory(); // Reload inventory
    } catch (err) {
        modalAlert.textContent = err.message;
        modalAlert.style.display = 'block';
    }
}

// Sidebar navigation
function showSection(sectionId, element) {
    document.querySelectorAll('.content-section').forEach(sec => sec.classList.remove('active-section'));
    document.getElementById(sectionId).classList.add('active-section');
    
    document.querySelectorAll('.sidebar-item').forEach(item => item.classList.remove('active'));
    element.classList.add('active');

    if (sectionId === 'section-requests') {
        fetchRequests();
    } else if (sectionId === 'section-returns') {
        fetchReturns();
    } else if (sectionId === 'section-history') {
        fetchHistory();
    } else if (sectionId === 'section-student-history') {
        fetchStudentHistory();
    } else if (sectionId === 'section-stats') {
        fetchEstadisticas();
    } else if (sectionId === 'section-bloqueados') {
        fetchBloqueados();
    } else if (sectionId === 'section-categories') {
        fetchCategories();
    } else if (sectionId === 'section-audit') {
        fetchAuditLogs();
    }
}

// Fetch Admin requests
async function fetchRequests() {
    const container = document.getElementById('requestsContainer');
    container.innerHTML = '<p style="color: var(--text-secondary);">Cargando solicitudes...</p>';

    try {
        const response = await fetch('/api/prestamosadmin/pendientes', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        if (response.ok) {
            currentRequests = await response.json();
            renderRequests(currentRequests);
        } else {
            container.innerHTML = '<p style="color: var(--color-danger);">Error al cargar solicitudes.</p>';
        }
    } catch (err) {
        container.innerHTML = '<p style="color: var(--color-danger);">Error de conexión.</p>';
    }
}

// Render Request Cards
function renderRequests(requests) {
    const container = document.getElementById('requestsContainer');
    container.innerHTML = '';

    if (requests.length === 0) {
        container.innerHTML = '<div class="glass-panel" style="padding: 30px; text-align: center; color: var(--text-secondary);">No hay solicitudes pendientes.</div>';
        return;
    }

    requests.forEach(req => {
        const card = document.createElement('div');
        card.className = 'item-card glass-panel';
        
        card.innerHTML = `
            <div>
                <div class="card-header">
                    <span class="item-code" style="background: rgba(245, 158, 11, 0.1); color: var(--color-warning); border-color: rgba(245, 158, 11, 0.2);">
                        ${req.codigoReserva}
                    </span>
                    <span class="item-status status-prestado">${new Date(req.fechaSolicitud).toLocaleDateString()}</span>
                </div>
                <h3 class="item-name">${req.nombreItem} ${req.modeloItem ? '(' + req.modeloItem + ')' : ''}</h3>
                <p class="item-desc" style="margin-bottom: 5px;">
                    <strong>Solicitante:</strong> ${req.usuario}
                </p>
            </div>
            <div class="card-actions" style="margin-top: 15px;">
                <button onclick="openRequestDetailsModal('${req.codigoReserva}')" class="btn btn-primary" style="width: 100%;">Ver Solicitud Completa</button>
            </div>
        `;
        container.appendChild(card);
    });
}

function openRequestDetailsModal(codigoReserva) {
    const req = currentRequests.find(r => r.codigoReserva === codigoReserva);
    if (!req) return;
    
    document.getElementById('reqDetComment').value = '';
    
    const content = `
        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 15px; margin-bottom: 15px;">
            <div>
                <strong style="color: var(--text-secondary); display: block; font-size: 0.8rem; text-transform: uppercase;">Solicitante</strong>
                <span style="font-weight: 500;">${req.usuario}</span>
            </div>
            <div>
                <strong style="color: var(--text-secondary); display: block; font-size: 0.8rem; text-transform: uppercase;">Código Reserva</strong>
                <span style="font-weight: 500; color: var(--color-warning);">${req.codigoReserva}</span>
            </div>
        </div>
        
        <div style="background: rgba(255,255,255,0.05); padding: 15px; border-radius: 8px; margin-bottom: 15px;">
            <strong style="color: var(--text-secondary); display: block; font-size: 0.8rem; text-transform: uppercase; margin-bottom: 5px;">Ítem Solicitado</strong>
            <span style="font-size: 1.1rem; font-weight: bold; color: var(--color-primary);">${req.nombreItem} ${req.modeloItem ? '(' + req.modeloItem + ')' : ''}</span>
            <div style="margin-top: 10px; font-weight: bold;">Cantidad: <span style="font-size: 1.2rem;">${req.cantidad}</span></div>
        </div>
        
        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 15px;">
            <div>
                <strong style="color: var(--text-secondary); display: block; font-size: 0.8rem; text-transform: uppercase;">Fecha Solicitud</strong>
                <span>${new Date(req.fechaSolicitud).toLocaleDateString()}</span>
            </div>
            <div>
                <strong style="color: var(--text-secondary); display: block; font-size: 0.8rem; text-transform: uppercase;">Devolución Esperada</strong>
                <span style="font-weight: bold; color: var(--color-warning);">${req.fechaDevolucion ? new Date(req.fechaDevolucion).toLocaleDateString() : 'No especificada'}</span>
            </div>
        </div>
    `;
    
    document.getElementById('reqDetContent').innerHTML = content;
    
    // Bind buttons
    document.getElementById('reqDetBtnAprobar').onclick = () => updateRequestStatusModal(req.codigoReserva, 'APROBADO');
    document.getElementById('reqDetBtnRechazar').onclick = () => updateRequestStatusModal(req.codigoReserva, 'RECHAZADO');
    
    document.getElementById('requestDetailsModal').style.display = 'flex';
}

function closeRequestDetailsModal() {
    document.getElementById('requestDetailsModal').style.display = 'none';
}

// Update Request Status from Modal (Approve/Reject)
async function updateRequestStatusModal(codigoReserva, estado) {
    let comentario = document.getElementById('reqDetComment').value.trim();
    if (comentario === '') comentario = null;

    try {
        const response = await fetch(`/api/prestamosadmin/${codigoReserva}/estado`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({ estado: estado, comentarioAdmin: comentario })
        });

        const data = await response.json();
        if (response.ok) {
            closeRequestDetailsModal();
            showToast(data.mensaje, 'success');
            fetchRequests(); // reload list
            fetchInventory(); // stock may have changed
        } else {
            showToast("Error: " + data.mensaje, 'error');
        }
    } catch (err) {
        showToast("Error de conexión al actualizar la solicitud.", 'error');
    }
}

async function deleteItem(id) {
    const confirmed = await showConfirm('¿Estás seguro de que deseas eliminar (dar de baja) este elemento?');
    if (!confirmed) return;

    try {
        const response = await fetch(`/api/inventario/${id}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        const data = await response.json();
        if (!response.ok) {
            throw new Error(data.mensaje || 'Error al eliminar el elemento.');
        }

        showToast('Elemento dado de baja correctamente.', 'success');
        await fetchInventory(); // Reload inventory
    } catch (error) {
        showToast(error.message, 'error');
    }
}

function agregarStock(id) {
    document.getElementById('addStockItemId').value = id;
    document.getElementById('addStockAmount').value = 1;
    document.getElementById('addStockModal').style.display = 'flex';
}

function closeAddStockModal() {
    document.getElementById('addStockModal').style.display = 'none';
}

async function confirmAgregarStock() {
    const id = document.getElementById('addStockItemId').value;
    const cantidad = document.getElementById('addStockAmount').value;

    const cantidadNum = parseInt(cantidad);
    if (isNaN(cantidadNum) || cantidadNum <= 0) {
        showToast('La cantidad debe ser un número mayor a 0.', 'error');
        return;
    }

    try {
        const response = await fetch(`/api/inventario/${id}/agregar-stock`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({ cantidad: cantidadNum })
        });

        const data = await response.json();
        if (response.ok) {
            closeAddStockModal();
            showToast(data.mensaje, 'success');
            fetchInventory();
        } else {
            showToast('Error: ' + data.mensaje, 'error');
        }
    } catch (error) {
        showToast('Error de conexión al agregar stock.', 'error');
    }
}

function abrirModalReparar(id, max) {
    document.getElementById('repairStockItemId').value = id;
    const inputAmount = document.getElementById('repairStockAmount');
    inputAmount.value = 1;
    inputAmount.max = max;
    document.getElementById('repairStockModal').style.display = 'flex';
}

function closeRepairStockModal() {
    document.getElementById('repairStockModal').style.display = 'none';
}

async function confirmarReparacion() {
    const id = document.getElementById('repairStockItemId').value;
    const inputAmount = document.getElementById('repairStockAmount');
    const cantidadNum = parseInt(inputAmount.value);
    const max = parseInt(inputAmount.max);

    if (isNaN(cantidadNum) || cantidadNum <= 0 || cantidadNum > max) {
        showToast(`La cantidad debe ser entre 1 y ${max}.`, 'error');
        return;
    }

    try {
        const response = await fetch(`/api/inventario/${id}/reparar-defectuoso`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({ cantidad: cantidadNum })
        });

        const data = await response.json();
        if (response.ok) {
            closeRepairStockModal();
            showToast(data.mensaje, 'success');
            fetchInventory();
        } else {
            showToast('Error: ' + data.mensaje, 'error');
        }
    } catch (error) {
        showToast('Error de conexión al reparar.', 'error');
    }
}

// Fetch Admin History
let currentHistory = [];

async function fetchHistory() {
    const tbody = document.getElementById('historyTableBody');
    tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; padding: 20px; color: var(--text-secondary);">Cargando historial...</td></tr>';

    try {
        const response = await fetch('/api/prestamosadmin/historial', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        if (response.ok) {
            currentHistory = await response.json();
            filterHistory(); // instead of renderHistory directly
        } else {
            tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; padding: 20px; color: var(--color-danger);">Error al cargar el historial.</td></tr>';
        }
    } catch (err) {
        tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; padding: 20px; color: var(--color-danger);">Error de conexión.</td></tr>';
    }
}

function filterHistory() {
    const dateFilter = document.getElementById('adminHistoryDateFilter').value;
    const statusFilter = document.getElementById('adminHistoryStatusFilter').value;
    const textFilter = document.getElementById('adminHistoryStudentFilter').value.toLowerCase().trim();

    const filtered = currentHistory.filter(req => {
        // Date match (req.fechaSolicitud or req.fechaDevolucion can be matched. We'll use fechaSolicitud for simplicity, or both)
        let matchesDate = true;
        if (dateFilter) {
            const reqDate = req.fechaSolicitud.split('T')[0];
            matchesDate = (reqDate === dateFilter);
        }

        let matchesStatus = true;
        if (statusFilter) {
            matchesStatus = (req.estado === statusFilter);
        }

        let matchesText = true;
        if (textFilter) {
            matchesText = req.usuario.toLowerCase().includes(textFilter) || 
                          req.codigoReserva.toLowerCase().includes(textFilter) ||
                          req.nombreItem.toLowerCase().includes(textFilter);
        }

        return matchesDate && matchesStatus && matchesText;
    });

    renderHistory(filtered);
}

function renderHistory(historyList) {
    const tbody = document.getElementById('historyTableBody');
    tbody.innerHTML = '';

    if (historyList.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; padding: 20px; color: var(--text-secondary);">No hay historial disponible.</td></tr>';
        return;
    }

    historyList.forEach(req => {
        const tr = document.createElement('tr');
        tr.style.borderBottom = '1px solid var(--border-color)';
        
        let statusClass = 'status-disponible';
        if (req.estado === 'APROBADO') statusClass = 'status-prestado';
        else if (req.estado === 'RECHAZADO') statusClass = 'status-baja';
        else if (req.estado === 'PENDIENTE') statusClass = 'status-baja'; // Or some other class
        
        tr.innerHTML = `
            <td style="padding: 10px;">${req.codigoReserva}</td>
            <td style="padding: 10px;">${req.usuario.split(' (')[0]}</td>
            <td style="padding: 10px;">${req.nombreItem} ${req.modeloItem ? '(' + req.modeloItem + ')' : ''}</td>
            <td style="padding: 10px;">${req.cantidad}</td>
            <td style="padding: 10px;"><span class="item-status ${statusClass}">${req.estado}</span></td>
            <td style="padding: 10px;">
                <button onclick="openHistoryDetailsModal('${req.codigoReserva}')" class="btn btn-secondary" style="padding: 4px 8px; font-size: 0.8rem;">Más información</button>
            </td>
        `;
        tbody.appendChild(tr);
    });
}

function openHistoryDetailsModal(codigoReserva) {
    const req = currentHistory.find(r => r.codigoReserva === codigoReserva);
    if (!req) return;
    
    const content = `
        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 15px; margin-bottom: 15px;">
            <div>
                <strong style="color: var(--text-secondary); display: block; font-size: 0.8rem; text-transform: uppercase;">Estudiante</strong>
                <span style="font-weight: 500;">${req.usuario}</span>
            </div>
            <div>
                <strong style="color: var(--text-secondary); display: block; font-size: 0.8rem; text-transform: uppercase;">Código Reserva</strong>
                <span style="font-weight: 500; color: var(--color-warning);">${req.codigoReserva}</span>
            </div>
        </div>
        
        <div style="background: rgba(255,255,255,0.05); padding: 15px; border-radius: 8px; margin-bottom: 15px;">
            <strong style="color: var(--text-secondary); display: block; font-size: 0.8rem; text-transform: uppercase; margin-bottom: 5px;">Ítem</strong>
            <span style="font-size: 1.1rem; font-weight: bold; color: var(--color-primary);">${req.nombreItem} ${req.modeloItem ? '(' + req.modeloItem + ')' : ''}</span>
            <div style="margin-top: 10px; font-weight: bold;">Cantidad: <span style="font-size: 1.2rem;">${req.cantidad}</span></div>
            <div style="margin-top: 10px; font-weight: bold;">Estado Actual: <span style="font-size: 1.1rem; color: var(--color-warning);">${req.estado}</span></div>
        </div>
        
        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 15px;">
            <div>
                <strong style="color: var(--text-secondary); display: block; font-size: 0.8rem; text-transform: uppercase;">Fecha Solicitud</strong>
                <span>${new Date(req.fechaSolicitud).toLocaleDateString()}</span>
            </div>
            <div>
                <strong style="color: var(--text-secondary); display: block; font-size: 0.8rem; text-transform: uppercase;">Devolución Estimada/Real</strong>
                <span style="font-weight: bold; color: var(--color-warning);">${req.fechaDevolucion ? new Date(req.fechaDevolucion).toLocaleDateString() : 'N/A'}</span>
            </div>
        </div>
        
        ${req.comentarioAdmin ? `
        <div style="background: rgba(244, 67, 54, 0.1); padding: 15px; border-radius: 8px; margin-top: 15px; border-left: 4px solid var(--color-danger);">
            <strong style="color: var(--color-danger); display: block; font-size: 0.8rem; text-transform: uppercase; margin-bottom: 5px;">Comentario del Administrador</strong>
            <span style="color: var(--text-primary); font-size: 0.95rem;">${req.comentarioAdmin}</span>
        </div>` : ''}
        
        ${req.evidenciaUrl ? `
        <div style="margin-top: 15px; text-align: center;">
            <strong style="display: block; font-size: 0.8rem; text-transform: uppercase; margin-bottom: 5px;">Evidencia Fotográfica</strong>
            <img src="${req.evidenciaUrl}" style="max-height: 200px; border-radius: 8px; border: 1px solid var(--border-color);">
        </div>` : ''}
    `;
    
    document.getElementById('histDetContent').innerHTML = content;
    
    const cantGroup = document.getElementById('histDetCantGroup');
    const commentGroup = document.getElementById('histDetCommentGroup');
    const imageGroup = document.getElementById('histDetImageGroup');
    const btnConfirmar = document.getElementById('histDetBtnConfirmar');
    const inputComment = document.getElementById('histDetComment');
    const btnRechazar = document.getElementById('histDetBtnRechazar');
    const inputCant = document.getElementById('histDetCantDefectuosa');
    
    inputComment.value = '';
    inputCant.value = '0';
    inputCant.max = req.cantidad;
    document.getElementById('histDetImage').value = '';
    
    if (req.estado === 'PENDIENTE_DEVOLUCION') {
        cantGroup.style.display = 'block';
        commentGroup.style.display = 'block';
        imageGroup.style.display = 'block';
        btnConfirmar.style.display = 'inline-block';
        btnRechazar.style.display = 'inline-block';
        btnConfirmar.onclick = () => marcarComoDevuelto(req.codigoReserva, req.cantidad);
        btnRechazar.onclick = () => rechazarDevolucion(req.codigoReserva);
    } else if (req.estado === 'APROBADO') {
        cantGroup.style.display = 'block';
        commentGroup.style.display = 'block';
        imageGroup.style.display = 'block';
        btnConfirmar.style.display = 'inline-block';
        btnRechazar.style.display = 'none';
        btnConfirmar.onclick = () => marcarComoDevuelto(req.codigoReserva, req.cantidad);
    } else {
        cantGroup.style.display = 'none';
        commentGroup.style.display = 'none';
        imageGroup.style.display = 'none';
        btnConfirmar.style.display = 'none';
        btnRechazar.style.display = 'none';
    }
    
    document.getElementById('historyDetailsModal').style.display = 'flex';
}

function closeHistoryDetailsModal() {
    document.getElementById('historyDetailsModal').style.display = 'none';
}

async function marcarComoDevuelto(codigoReserva, cantidadTotal) {
    let comentario = document.getElementById('histDetComment').value.trim();
    let cantDefectuosa = parseInt(document.getElementById('histDetCantDefectuosa').value) || 0;
    
    if (cantDefectuosa < 0 || cantDefectuosa > cantidadTotal) {
        showToast('La cantidad defectuosa es inválida.', 'error');
        return;
    }

    if (comentario === '') comentario = null;
    
    if (cantDefectuosa > 0 && !comentario) {
        showToast('Debes ingresar un comentario justificando los elementos defectuosos.', 'error');
        return;
    }

    try {
        let evidenciaUrl = null;
        const fileInput = document.getElementById('histDetImage');
        if (fileInput && fileInput.files.length > 0) {
            const formData = new FormData();
            formData.append('file', fileInput.files[0]);
            const uploadRes = await fetch('/api/archivos/upload/evidencias', {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${token}` },
                body: formData
            });
            const uploadData = await uploadRes.json();
            if (!uploadRes.ok) {
                throw new Error(uploadData.mensaje || 'Error al subir la evidencia.');
            }
            evidenciaUrl = uploadData.url;
        }

        const response = await fetch(`/api/prestamosadmin/${codigoReserva}/devolver`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({ comentarioAdmin: comentario, cantidadDefectuosa: cantDefectuosa, evidenciaUrl: evidenciaUrl })
        });

        const data = await response.json();
        if (response.ok) {
            closeHistoryDetailsModal();
            showToast(data.mensaje, 'success');
            fetchHistory(); // reload history
            fetchReturns(); // reload return requests if needed
            fetchInventory(); // stock may have changed
        } else {
            showToast("Error: " + data.mensaje, 'error');
        }
    } catch (err) {
        showToast("Error de conexión al marcar como devuelto.", 'error');
    }
}

async function rechazarDevolucion(codigoReserva) {
    let comentario = document.getElementById('histDetComment').value.trim();
    if (!comentario) {
        showToast("Debe agregar un comentario explicando por qué rechaza la devolución.", 'error');
        return;
    }

    try {
        let evidenciaUrl = null;
        const fileInput = document.getElementById('histDetImage');
        if (fileInput && fileInput.files.length > 0) {
            const formData = new FormData();
            formData.append('file', fileInput.files[0]);
            const uploadRes = await fetch('/api/archivos/upload/evidencias', {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${token}` },
                body: formData
            });
            const uploadData = await uploadRes.json();
            if (!uploadRes.ok) {
                throw new Error(uploadData.mensaje || 'Error al subir la evidencia.');
            }
            evidenciaUrl = uploadData.url;
        }

        const response = await fetch(`/api/prestamosadmin/${codigoReserva}/devolver/rechazar`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({ comentarioAdmin: comentario, evidenciaUrl: evidenciaUrl })
        });
        const data = await response.json();
        if (response.ok) {
            closeHistoryDetailsModal();
            showToast(data.mensaje, 'success');
            fetchReturns(); // Refresh Returns tab
            fetchHistory(); // Refresh History tab
        } else {
            showToast("Error: " + data.mensaje, 'error');
        }
    } catch (err) {
        showToast("Error de conexión al rechazar devolución.", 'error');
    }
}

// Admin Returns Logic
let currentReturns = [];
async function fetchReturns() {
    const container = document.getElementById('returnsContainer');
    container.innerHTML = '<p style="color: var(--text-secondary);">Cargando devoluciones...</p>';

    try {
        const response = await fetch('/api/prestamosadmin/devoluciones', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        if (response.ok) {
            currentReturns = await response.json();
            // Append to currentHistory so openHistoryDetailsModal can find it
            currentReturns.forEach(ret => {
                if (!currentHistory.find(h => h.codigoReserva === ret.codigoReserva)) {
                    currentHistory.push(ret);
                }
            });
            renderReturns(currentReturns);
        } else {
            container.innerHTML = '<p style="color: var(--color-danger);">Error al cargar devoluciones.</p>';
        }
    } catch (err) {
        container.innerHTML = '<p style="color: var(--color-danger);">Error de conexión.</p>';
    }
}

function renderReturns(returns) {
    const container = document.getElementById('returnsContainer');
    container.innerHTML = '';

    if (returns.length === 0) {
        container.innerHTML = '<div class="glass-panel" style="padding: 30px; text-align: center; color: var(--text-secondary);">No hay solicitudes de devolución pendientes.</div>';
        return;
    }

    returns.forEach(req => {
        const card = document.createElement('div');
        card.className = 'item-card glass-panel';
        
        card.innerHTML = `
            <div>
                <div class="card-header">
                    <span class="item-code" style="background: rgba(56, 189, 248, 0.1); color: var(--color-primary); border-color: rgba(56, 189, 248, 0.2);">
                        ${req.codigoReserva}
                    </span>
                    <span class="item-status" style="background: rgba(245, 158, 11, 0.1); color: var(--color-warning);">PENDIENTE DEV</span>
                </div>
                <h3 class="item-name">${req.nombreItem} ${req.modeloItem ? '(' + req.modeloItem + ')' : ''}</h3>
                <p class="item-desc" style="margin-bottom: 5px;">
                    <strong>Solicitante:</strong> ${req.usuario}
                </p>
                <div style="margin-top: 10px; font-weight: bold;">Cantidad a devolver: <span style="font-size: 1.2rem;">${req.cantidad}</span></div>
            </div>
            <div class="card-actions" style="margin-top: 15px;">
                <button onclick="openHistoryDetailsModal('${req.codigoReserva}')" class="btn btn-primary" style="width: 100%;">Procesar Devolución</button>
            </div>
        `;
        container.appendChild(card);
    });
}

// Student History Logic
let currentStudentHistory = [];
async function fetchStudentHistory() {
    const tbody = document.getElementById('studentHistoryTableBody');
    tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; padding: 20px;">Cargando historial...</td></tr>';

    try {
        const response = await fetch('/api/estudiante/mis-solicitudes', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        if (response.ok) {
            currentStudentHistory = await response.json();
            filterStudentHistory(); // instead of renderStudentHistory directly
        } else {
            tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; padding: 20px; color: var(--color-danger);">Error al cargar historial.</td></tr>';
        }
    } catch (err) {
        tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; padding: 20px; color: var(--color-danger);">Error de conexión.</td></tr>';
    }
}

function filterStudentHistory() {
    const dateFilter = document.getElementById('studentHistoryDateFilter').value;
    const statusFilter = document.getElementById('studentHistoryStatusFilter').value;
    const textFilter = document.getElementById('studentHistoryItemFilter').value.toLowerCase().trim();

    const filtered = currentStudentHistory.filter(req => {
        let matchesDate = true;
        if (dateFilter) {
            const reqDate = req.fechaSolicitud.split('T')[0];
            matchesDate = (reqDate === dateFilter);
        }

        let matchesStatus = true;
        if (statusFilter) {
            matchesStatus = (req.estado === statusFilter);
        }

        let matchesText = true;
        if (textFilter) {
            matchesText = req.codigoReserva.toLowerCase().includes(textFilter) ||
                          req.nombreItem.toLowerCase().includes(textFilter);
        }

        return matchesDate && matchesStatus && matchesText;
    });

    renderStudentHistory(filtered);
}

function renderStudentHistory(historyList) {
    const tbody = document.getElementById('studentHistoryTableBody');
    tbody.innerHTML = '';

    if (historyList.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; padding: 20px; color: var(--text-secondary);">No tienes préstamos registrados.</td></tr>';
        return;
    }

    historyList.forEach(req => {
        const tr = document.createElement('tr');
        tr.style.borderBottom = '1px solid var(--border-color)';
        
        let statusClass = 'status-disponible';
        if (req.estado === 'APROBADO') statusClass = 'status-prestado';
        else if (req.estado === 'RECHAZADO') statusClass = 'status-baja';
        else if (req.estado === 'PENDIENTE') statusClass = 'status-baja'; // orange-ish ideally
        else if (req.estado === 'PENDIENTE_DEVOLUCION') statusClass = 'status-prestado';
        
        let actionButtons = `<button onclick="openStudentHistoryModal('${req.codigoReserva}')" class="btn btn-secondary" style="padding: 4px 8px; font-size: 0.8rem;">Más información</button>`;
        
        if (req.estado === 'APROBADO') {
            actionButtons += ` <button onclick="solicitarDevolucion('${req.codigoReserva}')" class="btn btn-primary" style="padding: 4px 8px; font-size: 0.8rem; margin-left: 5px; background: var(--color-warning); border-color: var(--color-warning);">Solicitar Devolución</button>`;
        }

        tr.innerHTML = `
            <td style="padding: 10px;">${req.codigoReserva}</td>
            <td style="padding: 10px;">${req.nombreItem} ${req.modeloItem ? '(' + req.modeloItem + ')' : ''}</td>
            <td style="padding: 10px;">${req.cantidad}</td>
            <td style="padding: 10px;">${new Date(req.fechaSolicitud).toLocaleDateString()}</td>
            <td style="padding: 10px;"><span class="item-status ${statusClass}">${req.estado}</span></td>
            <td style="padding: 10px;">
                ${actionButtons}
            </td>
        `;
        tbody.appendChild(tr);
    });
}

function openStudentHistoryModal(codigoReserva) {
    const req = currentStudentHistory.find(r => r.codigoReserva === codigoReserva);
    if (!req) return;
    
    if (!currentHistory.find(h => h.codigoReserva === codigoReserva)) {
        currentHistory.push(req);
    }
    
    openHistoryDetailsModal(codigoReserva);
    document.getElementById('histDetCantGroup').style.display = 'none';
    document.getElementById('histDetCommentGroup').style.display = 'none';
    document.getElementById('histDetBtnConfirmar').style.display = 'none';
}

async function solicitarDevolucion(codigoReserva) {
    const confirmed = await showConfirm('¿Estás seguro de solicitar la devolución de este ítem? Debes acercarte al laboratorio físico a entregarlo.');
    if (!confirmed) return;

    try {
        const response = await fetch(`/api/estudiante/prestamo/${codigoReserva}/devolver`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        const data = await response.json();
        if (response.ok) {
            showToast(data.mensaje, 'success');
            fetchStudentHistory(); // Reload table
        } else {
            showToast('Error: ' + data.mensaje, 'error');
        }
    } catch (error) {
        showToast('Error de conexión al procesar la devolución.', 'error');
    }
}

// --- ESTADÍSTICAS ---
let mainChartInstance = null;
let statsData = null;

async function fetchEstadisticas() {
    try {
        const response = await fetch('/api/estadisticas', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (response.ok) {
            statsData = await response.json();
            
            const isStudent = localStorage.getItem('user_role_id') === '2'; // Estudiante
            if (!isStudent && statsData.porEstudiante) {
                document.getElementById('optStatEstudiante').style.display = 'block';
            } else {
                document.getElementById('optStatEstudiante').style.display = 'none';
            }
            
            renderSelectedStatistic();
        } else {
            showToast('Error al cargar estadísticas.', 'error');
        }
    } catch (err) {
        console.error('Error fetching stats', err);
    }
}

function renderSelectedStatistic() {
    if (!statsData) return;
    
    const selector = document.getElementById('statsTypeSelector');
    const selectedType = selector.value;
    
    if (mainChartInstance) {
        mainChartInstance.destroy();
    }
    
    const ctx = document.getElementById('mainStatsChart').getContext('2d');
    
    const chartOptions = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: { labels: { color: '#ccc' } }
        },
        scales: {
            x: { ticks: { color: '#ccc' } },
            y: { ticks: { color: '#ccc' }, beginAtZero: true }
        }
    };
    
    let labels = [];
    let dataSet = [];
    let labelTitle = '';
    let bgColor = '';
    
    if (selectedType === 'elemento' && statsData.porElemento) {
        labels = statsData.porElemento.map(x => x.nombre);
        dataSet = statsData.porElemento.map(x => x.cantidad);
        labelTitle = 'Cantidad Devuelta por Elemento';
        bgColor = 'rgba(52, 152, 219, 0.7)';
    } else if (selectedType === 'categoria' && statsData.porCategoria) {
        labels = statsData.porCategoria.map(x => x.nombre);
        dataSet = statsData.porCategoria.map(x => x.cantidad);
        labelTitle = 'Cantidad Devuelta por Categoría';
        bgColor = 'rgba(230, 126, 34, 0.7)';
    } else if (selectedType === 'fecha' && statsData.porFecha) {
        labels = statsData.porFecha.map(x => x.fecha);
        dataSet = statsData.porFecha.map(x => x.cantidad);
        labelTitle = 'Devoluciones por Fecha';
        bgColor = 'rgba(155, 89, 182, 0.7)';
    } else if (selectedType === 'estudiante' && statsData.porEstudiante) {
        labels = statsData.porEstudiante.map(x => x.nombre);
        dataSet = statsData.porEstudiante.map(x => x.cantidad);
        labelTitle = 'Total Devuelto por Estudiante';
        bgColor = 'rgba(46, 204, 113, 0.7)';
    }
    
    mainChartInstance = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: labelTitle,
                data: dataSet,
                backgroundColor: bgColor,
                borderWidth: 1
            }]
        },
        options: chartOptions
    });
}

// --- BLOQUEADOS ---
async function fetchBloqueados() {
    try {
        const tbody = document.getElementById('bloqueadosTableBody');
        tbody.innerHTML = '<tr><td colspan="4" style="text-align:center;">Cargando...</td></tr>';
        
        const response = await fetch('/api/bloqueados', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        if (response.ok) {
            const data = await response.json();
            tbody.innerHTML = '';
            
            if (data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="4" style="text-align:center;">No hay estudiantes bloqueados actualmente.</td></tr>';
                return;
            }
            
            data.forEach(est => {
                const tr = document.createElement('tr');
                const prestamosList = est.prestamosVencidos.map(p => `<div>[${p.codigoReserva}] - Cant: ${p.cantidad} (Debió dev: ${new Date(p.fechaDevolucion).toLocaleDateString()})</div>`).join('');
                
                tr.innerHTML = `
                    <td style="padding: 10px;">${est.nombre} ${est.apellido}</td>
                    <td style="padding: 10px;">${est.correo}</td>
                    <td style="padding: 10px;">${prestamosList}</td>
                    <td style="padding: 10px; text-align: center;">
                        <button onclick="notificarEstudiante(${est.usuarioId})" class="btn btn-primary" style="padding: 5px 10px; font-size: 0.85rem;">
                            🔔 Notificar
                        </button>
                    </td>
                `;
                tbody.appendChild(tr);
            });
        } else {
            showToast('Error al cargar bloqueados.', 'error');
        }
    } catch (err) {
        console.error('Error fetching bloqueados', err);
    }
}

async function notificarEstudiante(usuarioId) {
    if(!confirm('¿Deseas enviar una notificación de alerta a este estudiante?')) return;
    
    try {
        const response = await fetch(`/api/bloqueados/notificar/${usuarioId}`, {
            method: 'POST',
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        if (response.ok) {
            showToast('Notificación enviada correctamente.', 'success');
        } else {
            const errorText = await response.text();
            showToast('Error: ' + errorText, 'error');
        }
    } catch (err) {
        console.error('Error', err);
        showToast('Error de conexión', 'error');
    }
}

// --- Modals for Docente ---
function openItemInfoModal(id) {
    const item = allItems.find(i => i.itemId === id);
    if (!item) return;

    const content = document.getElementById('itemInfoContent');
    content.innerHTML = `
        <h4 style="color: var(--text-primary); margin-bottom: 10px; font-size: 1.2rem;">${item.nombre}</h4>
        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-bottom: 15px;">
            <div><strong>Marca:</strong><br> ${item.marca || 'N/A'}</div>
            <div><strong>Modelo:</strong><br> ${item.modelo || 'N/A'}</div>
            <div><strong>Categoría:</strong><br> ${item.categoria?.nombre || 'General'}</div>
            <div><strong>Ubicación:</strong><br> ${item.espacio?.nombre || 'Laboratorio'}</div>
        </div>
        <p><strong>Descripción Técnica:</strong></p>
        <div style="background: rgba(255,255,255,0.05); padding: 10px; border-radius: 6px; border: 1px solid var(--border-color); white-space: pre-wrap;">${item.descripcion || 'Sin descripción disponible.'}</div>
    `;

    document.getElementById('itemInfoModal').style.display = 'flex';
}

function closeItemInfoModal() {
    document.getElementById('itemInfoModal').style.display = 'none';
}

// --- Admin Categories ---
async function fetchCategories() {
    const container = document.getElementById('categoriesTableBody');
    if(!container) return;
    container.innerHTML = '<tr><td colspan="3">Cargando categorías...</td></tr>';

    try {
        const response = await fetch('/api/categorias', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        const data = await response.json();
        
        container.innerHTML = '';
        if(data.length === 0) {
            container.innerHTML = '<tr><td colspan="3">No hay categorías.</td></tr>';
            return;
        }

        data.forEach(cat => {
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td style="padding: 10px; border-bottom: 1px solid var(--border-color);">${cat.categoriaId}</td>
                <td style="padding: 10px; border-bottom: 1px solid var(--border-color);">${cat.nombre}</td>
                <td style="padding: 10px; border-bottom: 1px solid var(--border-color);">
                    <button class="btn btn-secondary" onclick="editCategory(${cat.categoriaId}, '${cat.nombre.replace(/'/g, "\\'")}')">Editar</button>
                    <button class="btn btn-secondary" style="background: var(--color-danger); color: white; border: none;" onclick="deleteCategory(${cat.categoriaId})">Eliminar</button>
                </td>
            `;
            container.appendChild(tr);
        });
    } catch (err) {
        container.innerHTML = '<tr><td colspan="3">Error al cargar categorías.</td></tr>';
    }
}

let isEditingCategory = false;

function openCategoryModal() {
    isEditingCategory = false;
    document.getElementById('categoryModalTitle').textContent = 'Nueva Categoría';
    document.getElementById('categoryId').value = '';
    document.getElementById('categoryName').value = '';
    document.getElementById('categoryModalAlert').style.display = 'none';
    document.getElementById('categoryModal').style.display = 'flex';
}

function closeCategoryModal() {
    document.getElementById('categoryModal').style.display = 'none';
}

function editCategory(id, nombre) {
    isEditingCategory = true;
    document.getElementById('categoryModalTitle').textContent = 'Editar Categoría';
    document.getElementById('categoryId').value = id;
    document.getElementById('categoryName').value = nombre;
    document.getElementById('categoryModalAlert').style.display = 'none';
    document.getElementById('categoryModal').style.display = 'flex';
}

async function handleCategorySubmit(e) {
    e.preventDefault();
    const id = document.getElementById('categoryId').value;
    const nombre = document.getElementById('categoryName').value;
    const url = isEditingCategory ? `/api/categorias/${id}` : '/api/categorias';
    const method = isEditingCategory ? 'PUT' : 'POST';

    try {
        const response = await fetch(url, {
            method: method,
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ CategoriaId: isEditingCategory ? parseInt(id) : 0, Nombre: nombre })
        });

        if (response.ok) {
            closeCategoryModal();
            fetchCategories();
            showToast('Categoría guardada exitosamente.', 'success');
            await fetchInventory(); // Refresh main inventory metadata
        } else {
            const data = await response.json();
            document.getElementById('categoryModalAlert').textContent = data.mensaje || 'Error al guardar.';
            document.getElementById('categoryModalAlert').style.display = 'block';
        }
    } catch (error) {
        document.getElementById('categoryModalAlert').textContent = 'Error de red.';
        document.getElementById('categoryModalAlert').style.display = 'block';
    }
}

async function deleteCategory(id) {
    if (!confirm('¿Estás seguro de que deseas eliminar esta categoría? (Podría afectar ítems existentes)')) return;

    try {
        const response = await fetch(`/api/categorias/${id}`, {
            method: 'DELETE',
            headers: { 'Authorization': `Bearer ${token}` }
        });

        if (response.ok) {
            fetchCategories();
            showToast('Categoría eliminada.', 'success');
            await fetchInventory();
        } else {
            const data = await response.json();
            showToast(data.mensaje || 'Error al eliminar.', 'error');
        }
    } catch (error) {
        showToast('Error de red.', 'error');
    }
}

// --- Audit Logs ---
async function fetchAuditLogs() {
    const container = document.getElementById('auditTableBody');
    if(!container) return;
    container.innerHTML = '<tr><td colspan="5">Cargando logs...</td></tr>';

    try {
        const response = await fetch('/api/auditoria', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        const data = await response.json();
        
        container.innerHTML = '';
        if(data.length === 0) {
            container.innerHTML = '<tr><td colspan="5">No hay logs registrados.</td></tr>';
            return;
        }

        data.forEach(log => {
            const tr = document.createElement('tr');
            const d = new Date(log.fechaHora);
            tr.innerHTML = `
                <td style="padding: 10px; border-bottom: 1px solid var(--border-color);">${d.toLocaleDateString()} ${d.toLocaleTimeString()}</td>
                <td style="padding: 10px; border-bottom: 1px solid var(--border-color);">${log.usuario}</td>
                <td style="padding: 10px; border-bottom: 1px solid var(--border-color);">${log.accion}</td>
                <td style="padding: 10px; border-bottom: 1px solid var(--border-color);">${log.detalle}</td>
                <td style="padding: 10px; border-bottom: 1px solid var(--border-color);">${log.referenciaId || '-'}</td>
            `;
            container.appendChild(tr);
        });
    } catch (err) {
        container.innerHTML = '<tr><td colspan="5">Error al cargar logs.</td></tr>';
    }
}
