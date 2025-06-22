let selectedTests = [];

// Initialize
$(function () {
    calculatePricing();

    // Image preview
    $('#thumbnailInput').on('change', function () {
        const file = this.files[0];
        const allowedTypes = ['image/jpeg', 'image/png'];
        const maxSize = 5 * 1024 * 1024;

        if (!file) return;

        if (!allowedTypes.includes(file.type)) {
            alert('Chỉ hỗ trợ định dạng JPG, PNG!');
            return;
        }

        if (file.size > maxSize) {
            alert('Kích thước ảnh vượt quá 5MB!');
            return;
        }

        const reader = new FileReader();
        reader.onload = function (e) {
            $('#thumbnailPreview').attr('src', e.target.result);
        };
        reader.readAsDataURL(file);
    });

    // Form validation
    $('#createPackageForm').on('submit', function (e) {
        if (selectedTests.length === 0) {
            e.preventDefault();
            alert('Vui lòng chọn ít nhất một xét nghiệm cho gói khám!');
        }
    });
});

// Theme test vào package
function addTest() {
    const $select = $('#availableTests');
    const selectedOption = $select.find(':selected');

    if (!$select.val()) {
        alert("Vui lòng chọn xét nghiệm!");
        return;
    }

    const testId = parseInt(selectedOption.val());
    if (selectedTests.find(t => t.id === testId)) {
        alert('Xét nghiệm này đã được thêm!');
        return;
    }

    const test = {
        id: testId,
        name: selectedOption.data('name'),
        price: parseInt(selectedOption.data('price')),
        description: selectedOption.data('description')
    };

    selectedTests.push(test);
    renderSelectedTests();
    calculatePricing();
    $select.prop('selectedIndex', 0);
}

// Remove test from package
function removeTest(testId) {
    selectedTests = selectedTests.filter(t => t.id !== testId);
    renderSelectedTests();
    calculatePricing();
}

// Render selected tests
function renderSelectedTests() {
    const $container = $('#selectedTests');
    if (selectedTests.length === 0) {
        $container.html(`
                    <div class="text-center py-4 text-muted">
                        <i class="fas fa-flask fa-2x mb-2"></i>
                        <p>Chưa có xét nghiệm nào được chọn</p>
                    </div>
                `);
        updateHiddenFields();
        return;
    }

    let html = '';
    selectedTests.forEach(test => {
        html += `
                    <div class="test-item">
                        <div class="test-info">
                            <h6>${test.name}</h6>
                            <span>Mã: XN${test.id.toString().padStart(3, '0')}</span>
                            <p>Mô tả: ${test.description}</p >
                        </div>
                        <div class="d-flex align-items-center">
                            <span class="test-price me-3">${formatPrice(test.price)}</span>
                            <button type="button" class="btn-remove-test" onclick="removeTest(${test.id})">
                                <i class="fas fa-times"></i>
                            </button>
                        </div>
                    </div>
                `;
    });

    $container.html(html);
    updateHiddenFields();
    updateTestSelectOptions();
}

// Update hidden fields
function updateHiddenFields() {
    const $container = $('#hiddenTestFields');
    let html = '';
    selectedTests.forEach((test, index) => {
        html += `<input type="hidden" name="SelectedTestIds[${index}]" value="${test.id}" />`;
    });
    $container.html(html);
}

// Tính toán giá
function calculatePricing() {
    const totalTestsPrice = selectedTests.reduce((sum, test) => sum + test.price, 0);
    const discountPercent = parseFloat($('input[name="DiscountPercent"]').val()) || 0;
    const discountAmount = totalTestsPrice * (discountPercent / 100);
    const finalPrice = totalTestsPrice - discountAmount;

    $('#totalTestsPrice').text(formatPrice(totalTestsPrice));
    $('#discountAmount').text(formatPrice(discountAmount));
    $('#finalPrice').text(formatPrice(finalPrice));
}
function updateTestSelectOptions() {
    const $select = $('#availableTests');
    const selectedIds = selectedTests.map(t => t.id);

    $select.find('option').each(function () {
        const option = $(this);
        const optionVal = parseInt(option.val());

        if (isNaN(optionVal)) return; // Bỏ qua option mặc định "-- Chọn xét nghiệm --"

        if (selectedIds.includes(optionVal)) {
            option.hide();
        } else {
            option.show();
        }
    });

    // Reset lại select về mặc định
    $select.prop('selectedIndex', 0);
}

// Format giá
function formatPrice(price) {
    return new Intl.NumberFormat('vi-VN').format(price) + ' đ';
}