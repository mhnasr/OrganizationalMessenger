// ============================================
// Image Preview & Zoom
// ============================================

import { currentPreviewImage, setCurrentPreviewImage, setIsZoomed } from './variables.js';

export function openImagePreview(url) {
    const modal = document.createElement('div');
    modal.className = 'image-preview-modal';
    modal.innerHTML = `
        <div class="image-preview-overlay">
            <button class="close-preview" onclick="window.closeImagePreview(); event.stopPropagation()">✕</button>
            
            <button class="scroll-top-btn" id="scrollTopBtn" onclick="window.scrollToTopPreview(); event.stopPropagation()" style="display: none;">
                <i class="fas fa-arrow-up"></i>
            </button>
            
            <div class="image-preview-container" id="imageContainer">
                <img id="previewImage" src="${url}" alt="Preview">
            </div>
            
            <div class="image-preview-controls">
                <div class="zoom-control">
                    <i class="fas fa-search-minus"></i>
                    <input type="range" 
                           id="zoomSlider" 
                           min="100" 
                           max="300" 
                           value="100" 
                           step="10"
                           oninput="window.updateZoom(this.value)">
                    <i class="fas fa-search-plus"></i>
                    <span id="zoomValue">100%</span>
                </div>
                <a href="${url}" download class="download-btn" onclick="event.stopPropagation()">
                    <i class="fas fa-download"></i> دانلود
                </a>
                <button class="reset-zoom-btn" onclick="window.resetZoom(); event.stopPropagation()">
                    <i class="fas fa-undo"></i> بازنشانی
                </button>
            </div>
        </div>
    `;
    document.body.appendChild(modal);
    document.body.style.overflow = 'hidden';

    setCurrentPreviewImage(document.getElementById('previewImage'));

    const container = document.getElementById('imageContainer');
    const scrollTopBtn = document.getElementById('scrollTopBtn');

    if (container && scrollTopBtn) {
        container.addEventListener('scroll', function () {
            if (this.scrollTop > 100) {
                scrollTopBtn.style.display = 'flex';
            } else {
                scrollTopBtn.style.display = 'none';
            }
        });
    }

    setTimeout(() => {
        modal.classList.add('active');
    }, 10);
}

export function closeImagePreview() {
    const modal = document.querySelector('.image-preview-modal');
    if (modal) {
        modal.classList.remove('active');
        setTimeout(() => {
            modal.remove();
            document.body.style.overflow = 'auto';
            setCurrentPreviewImage(null);
        }, 300);
    }
}

export function scrollToTopPreview() {
    const container = document.getElementById('imageContainer');
    if (container) {
        container.scrollTo({
            top: 0,
            left: 0,
            behavior: 'smooth'
        });
    }
}

export function updateZoom(value) {
    if (!currentPreviewImage) return;

    const scale = value / 100;
    const container = document.getElementById('imageContainer');

    const scrollTopBefore = container ? container.scrollTop : 0;
    const scrollLeftBefore = container ? container.scrollLeft : 0;

    currentPreviewImage.style.transform = `scale(${scale})`;

    const zoomValueEl = document.getElementById('zoomValue');
    if (zoomValueEl) {
        zoomValueEl.textContent = `${value}%`;
    }

    if (container && scale > 1) {
        setTimeout(() => {
            container.scrollTop = scrollTopBefore * (scale / (scale - 0.1));
            container.scrollLeft = scrollLeftBefore;
        }, 50);
    }

    console.log('🔍 Zoom:', value + '%');
}

export function resetZoom() {
    const slider = document.getElementById('zoomSlider');
    const container = document.getElementById('imageContainer');

    if (slider) {
        slider.value = 100;
        updateZoom(100);
    }

    if (container) {
        container.scrollTo({
            top: 0,
            left: 0,
            behavior: 'smooth'
        });
    }
}

// Escape key listener
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
        closeImagePreview();
    }
});

// Export to window
window.openImagePreview = openImagePreview;
window.closeImagePreview = closeImagePreview;
window.scrollToTopPreview = scrollToTopPreview;
window.updateZoom = updateZoom;
window.resetZoom = resetZoom;