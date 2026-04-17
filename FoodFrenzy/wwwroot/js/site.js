
function initializePriceRangeFilter() {
    const priceRange = document.getElementById('priceRange');
    const priceValue = document.getElementById('priceValue');

    if (priceRange && priceValue) {
        priceRange.addEventListener('input', function () {
            priceValue.textContent = `Rs. ${this.value}`;
        });

        priceRange.addEventListener('change', function () {
            document.getElementById('searchForm').submit();
        });
    }
}

// Category chips instead of dropdown (optional)
function initializeCategoryChips() {
    const categoryContainer = document.getElementById('categoryChips');
    if (!categoryContainer) return;

    // This would require additional backend endpoint to get categories
    fetch('/Explore/GetCategories')
        .then(response => response.json())
        .then(categories => {
            categories.forEach(category => {
                const chip = document.createElement('button');
                chip.type = 'button';
                chip.className = 'category-chip';
                chip.textContent = category;
                chip.dataset.category = category;

                chip.addEventListener('click', function () {
                    document.getElementById('category').value = category;
                    document.getElementById('searchForm').submit();
                });

                categoryContainer.appendChild(chip);
            });
        });
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    initializePriceRangeFilter();
    initializeCategoryChips();
});