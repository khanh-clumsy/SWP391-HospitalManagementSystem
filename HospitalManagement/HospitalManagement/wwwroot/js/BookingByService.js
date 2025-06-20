$(document).ready(function () {
    // Hide containers initially
    $('#package-tests-container').hide();
    $('.test-title').hide();

    // Toggle service / package dropdowns
    function toggleDropdowns(reset = true) {
        const isService = $('#serviceRadio').is(':checked');
        $('.service-dropdown').toggle(isService);
        $('.package-dropdown').toggle(!isService);

        if (reset) {
            // Chỉ clear khi thực sự toggle
            $('#serviceDropdown').val('');
            $('#packageDropdown').val('');
            $('#SelectedSlotId').val('');
            $('.test-title').hide();
            $('#package-tests-container').hide().empty();
        }
    }

    // Load time slots from server
    function loadTimeSlots() {
        const type = $('#serviceRadio').is(':checked') ? 'service' : 'package';
        const serviceId = $('#serviceDropdown').val();
        const packageId = $('#packageDropdown').val();
        const date = $('#appointmentDate').val();

        if (!date) {
            $('#time-slots-container').hide().empty();
            return;
        }

        $('#time-slots-container')
            .html('<div class="loading-slots"><div class="loading-spinner"></div>Đang tải...</div>')
            .show();

        const data = {
            date: date,
            SelectedServiceId: type === 'service' ? serviceId : null,
            SelectedPackageId: type === 'package' ? packageId : null
        };

        $.ajax({
            url: '/Appointment/GetSlots',
            method: 'GET',
            data: data,
            success: function (slots) {
                $('#time-slots-container').empty();
                if (!slots || slots.length === 0) {
                    $('#time-slots-container').html(
                        '<div class="no-slots"><i class="fas fa-calendar-times"></i><div>Không có khung giờ nào khả dụng</div></div>'
                    );
                    return;
                }
                let html = '';
                slots.forEach(slot => {
                    const slotClass = slot.isBooked ? 'time-slot booked' : 'time-slot available';
                    const statusText = slot.isBooked ? 'Đã đặt' : 'Có thể đặt';
                    const dotClass = slot.isBooked ? 'booked' : 'available';
                    html += `
                    <div class="${slotClass}" data-slot-id="${slot.slotId}">
                      <div class="fw-semibold">${slot.slotTime}</div>
                      <div class="slot-status">
                        <span class="status-dot ${dotClass}"></span>
                        <span>${statusText}</span>
                      </div>
                    </div>`;
                });
                $('#time-slots-container').html(html);
                $('#time-slots-container').show();

            },
            error: function () {
                $('#time-slots-container')
                    .css('display','grid')
                    .html('<div class="no-slots">Không thể tải dữ liệu khung giờ.</div>');
            }
        });
    }

    // Enable/disable submit button
    function updateSubmitButton() {
        const type = $('#serviceRadio').is(':checked') ? 'service' : 'package';
        const id = type === 'service' ? $('#serviceDropdown').val() : $('#packageDropdown').val();
        const isValid = $('#appointmentDate').val() && id && $('#SelectedSlotId').val();
        $('.submit-btn').prop('disabled', !isValid);
    }

    // When switching service/package
    $('input[name="ServiceType"]').on('change', function () {
        // Toggle chỉ clear dropdowns/tests thôi
        toggleDropdowns(true);

        // Luôn load lại slot ngay khi đổi radio
        loadTimeSlots();
        updateSubmitButton();

        // Nếu đang ở package và đã có package chọn sẵn thì trigger load tests
        if ($('#packageRadio').is(':checked') && $('#packageDropdown').val()) {
            $('#packageDropdown').trigger('change');
        }
    });


    // When user selects a service or package
    $(document).on('change', '#serviceDropdown, #packageDropdown', function () {
        const isPackage = this.id === 'packageDropdown';
        const id = $(this).val();
        console.log(`Selected ${isPackage ? 'package' : 'service'} ID:`, id);

        loadTimeSlots();
        updateSubmitButton();

        if (isPackage) {
            if (!id) {
                $('.test-title').hide();
                $('#package-tests-container').hide().empty();
                $('#time-slots-container').hide().empty();
                return;
            }
            $('.test-title').show();
            $('#package-tests-container')
                .html('<div class="text-center py-2"><i class="fas fa-spinner fa-spin"></i> Đang tải danh sách xét nghiệm...</div>')
                .show();

            $.getJSON('/Appointment/GetTestsByPackage', { packageId: id })
                .done(function (tests) {
                    console.log(tests);
                    if (!tests.length) {
                        $('#package-tests-container').html('<div class="text-muted">Gói này chưa có xét nghiệm nào.</div>');
                        return;
                    }
                    let html = '<ul class="list-group">';
                    tests.forEach(t => {
                        html += `<li class="list-group-item">${t.name}</li>`;
                    });
                    html += '</ul>';
                    $('#package-tests-container').html(html);
                })
                .fail(function () {
                    $('#package-tests-container').html('<div class="text-danger">Không thể tải danh sách xét nghiệm.</div>');
                });
        }
    });

    // When date changes
    $('#appointmentDate').on('change', function () {
        loadTimeSlots();
        updateSubmitButton();
    });

    // When a time slot is clicked
    $(document).on('click', '.time-slot.available', function () {
        $('.time-slot.available').removeClass('selected');
        $(this).addClass('selected');
        $('#SelectedSlotId').val($(this).data('slot-id'));
        updateSubmitButton();
    });

    // Form submission
    $('.submit-btn').on('click', function () {
        $(this).prop('disabled', true);
        $('#serviceForm').submit();
    });

    // Initial setup
    toggleDropdowns(false);
    updateSubmitButton();
    // If package preselected on page load, simulate change
    if ($('#packageRadio').is(':checked') && $('#packageDropdown').val()) {
        $('#packageDropdown').trigger('change');
    } else {
        loadTimeSlots();
    }
});
