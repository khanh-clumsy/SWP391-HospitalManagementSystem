$(document).ready(function () {
    // Navigation button event listeners
    $(document).on('click', '.carousel-nav.prev', prevDoctor);
    $(document).on('click', '.carousel-nav.next', nextDoctor);

    // Scroll functions
    function prevDoctor() {
        const container = document.querySelector('.doctor-scroll-container');
        if (container) {
            container.scrollBy({ left: -200, behavior: 'smooth' });
            setTimeout(updateDoctorScrollVisibility, 300);
            console.log('Scrolled left');
        } else {
            console.error('Scroll container not found');
        }
    }

    function nextDoctor() {
        const container = document.querySelector('.doctor-scroll-container');
        if (container) {
            container.scrollBy({ left: 200, behavior: 'smooth' });
            setTimeout(updateDoctorScrollVisibility, 300);
            console.log('Scrolled right');
        } else {
            console.error('Scroll container not found');
        }
    }

    // Update visibility of navigation buttons
    function updateDoctorScrollVisibility() {
        const container = document.querySelector('.doctor-scroll-container');
        if (!container) return;

        const prevButton = document.querySelector('.carousel-nav.prev');
        const nextButton = document.querySelector('.carousel-nav.next');

        // Log scroll info for debugging
        console.log('Scroll width:', container.scrollWidth, 'Client width:', container.clientWidth, 'Scroll left:', container.scrollLeft);

        // Show/hide buttons based on scroll position
        prevButton.style.display = container.scrollLeft <= 0 ? 'none' : 'block';
        nextButton.style.display = container.scrollLeft + container.clientWidth >= container.scrollWidth - 1 ? 'none' : 'block';
    }

    // Toggle between Service and Doctor forms
    $('.toggle-btn').on('click', function () {
        $('.toggle-btn').removeClass('active');
        $(this).addClass('active');
        const type = $(this).data('type');
        $('#serviceForm').toggle(type === 'service');
        $('#doctorForm').toggle(type === 'doctor');

        // Reset form fields
        $('.submit-btn').prop('disabled', true);
        $('#SelectedDoctorId').val('');
        $('#SelectedSlotId').val('');
        $('#serviceDropdown').val('');
        $('#serviceDropdownDoctor').val('');
        $('#noteService').val('');
        $('#noteDoctor').val('');
        $('#modelNote').val('');
        $('#doctorList').empty();
        $('.doctor-section').hide();
        $('#time-slots-container').empty();
        $('#chooseTimeTitle').hide();
        $('.service-dropdown').hide();
        $('.note-area').hide();

        if (type === 'service' && $('#appointmentDate').val()) {
            console.log('Service mode: Showing service-dropdown and note-area');
            $('.service-dropdown').show();
            $('.note-area').show();
        } else if (type === 'doctor' && $('#appointmentDate').val()) {
            console.log('Doctor mode: Triggering date change');
            $('#appointmentDate').trigger('change');
        }
    });

    // Handle date selection
    $('#appointmentDate').on('change', function () {
        const selectedDate = $(this).val();
        if (!selectedDate) {
            $('#doctorList').empty();
            $('.doctor-section').hide();
            $('#time-slots-container').empty();
            $('#chooseTimeTitle').hide();
            $('#SelectedDoctorId').val('');
            $('#SelectedSlotId').val('');
            $('#serviceDropdown').val('');
            $('#serviceDropdownDoctor').val('');
            $('#noteService').val('');
            $('#noteDoctor').val('');
            $('#modelNote').val('');
            $('.service-dropdown').hide();
            $('.note-area').hide();
            $('.submit-btn').prop('disabled', true);
            return;
        }

        if ($('#serviceForm').is(':visible')) {
            $('#serviceForm .service-dropdown').show();
            $('#serviceForm .note-area').show();
        } else if ($('#doctorForm').is(':visible')) {
            // Load available doctors
            $.ajax({
                url: '/Appointment/GetDoctorsByDate',
                type: 'GET',
                data: { date: selectedDate },
                success: function (data) {
                    $('#doctorList').empty();
                    if (data.length === 0) {
                        $('#doctorList').append('<p class="text-muted text-center my-3">Không có bác sĩ trống trong ngày này</p>');
                        $('.doctor-section').show();
                        updateDoctorScrollVisibility();
                        return;
                    }

                    data.forEach(function (doctor) {
                        const card = `
                            <div class="card text-center shadow-sm doctor-card" data-doctor-id="${doctor.doctorId}" style="min-width: 180px; cursor: pointer;">
                                <div class="card-body">
                                    <img src="${doctor.profileImage ? 'data:image/png;base64,' + doctor.profileImage : '/img/logo.jpg'}"
                                         class="img-fluid rounded-circle mb-2" style="width: 60px; height: 60px;" />
                                    <h6 class="card-title">${doctor.doctorName}</h6>
                                    <p class="text-muted small">${doctor.departmentName}</p>
                                </div>
                            </div>`;
                        $('#doctorList').append(card);
                    });
                    $('.doctor-section').show();
                    updateDoctorScrollVisibility();
                },
                error: function (xhr, status, error) {
                    console.error("Error loading doctors: ", status, error);
                }
            });
        }
    });

    // Handle doctor selection
    $(document).on('click', '.doctor-card', function () {
        $('.doctor-card').removeClass('selected');
        $(this).addClass('selected');

        const doctorId = $(this).data('doctor-id');
        $('#SelectedDoctorId').val(doctorId);
        console.log("Doctor ID: " + doctorId);
        $('#chooseTimeTitle').show();
        const selectedDate = $('#appointmentDate').val();
        if (selectedDate) {
            loadTimeSlots(doctorId, selectedDate);
        }
    });

    // Load time slots for selected doctor and date
    function loadTimeSlots(doctorId, selectedDate) {
        if (!doctorId || !selectedDate) return;

        $('#time-slots-container').html('<div class="loading-slots"><div class="loading-spinner"></div>Đang tải...</div>');

        $.ajax({
            url: '/Appointment/GetSlotsByDoctorAndDate',
            method: 'GET',
            data: { doctorId, date: selectedDate },
            success: function (slots) {
                $('#time-slots-container').empty();
                if (slots.length === 0) {
                    $('#time-slots-container').html('<div class="no-slots"><i class="fas fa-calendar-times"></i><div>Không có khung giờ nào khả dụng</div></div>');
                    return;
                }

                let html = '';
                slots.forEach(slot => {
                    const slotClass = slot.isBooked ? 'time-slot booked' : 'time-slot available';
                    const statusText = slot.isBooked ? 'Đã đặt' : 'Có thể đặt';
                    const dotClass = slot.isBooked ? 'booked' : 'available';

                    html += `
                        <div class="${slotClass}" data-slot-id="${slot.slotId}" data-time="${slot.slotTime}">
                            <div class="fw-semibold">${slot.slotTime}</div>
                            <div class="slot-status">
                                <span class="status-dot ${dotClass}"></span>
                                <span>${statusText}</span>
                            </div>
                        </div>`;
                });

                $('#time-slots-container').html(html);
            },
            error: function () {
                $('#time-slots-container').html('<div class="no-slots">Không thể tải dữ liệu khung giờ.</div>');
            }
        });
    }

    // Handle time slot selection
    $(document).on('click', '.time-slot.available', function () {
        $('.time-slot.available').removeClass('selected');
        $(this).addClass('selected');
        const slotId = $(this).data('slot-id');
        $('#SelectedSlotId').val(slotId);
        console.log("SlotID: " + slotId);
        $('#doctorForm .service-dropdown').show();
    });

    // Sync service selection from doctor mode to model
    $(document).on('change', '#serviceDropdownDoctor', function () {
        const serviceId = $(this).val();
        console.log("Selected Service ID: " + serviceId);
        $('#serviceDropdown').val(serviceId);
        $('#doctorForm .note-area').show();
    });

    // Handle service selection in service mode
    $(document).on('change', '#serviceDropdown', function () {
        const serviceId = $(this).val();
        console.log("Selected Service ID: " + serviceId);
        $('#serviceForm .note-area').show();
    });

    // Sync note from service and doctor modes to model
    $(document).on('input', '#noteService', function () {
        const note = $(this).val();
        $('#modelNote').val(note);
    });

    $(document).on('input', '#noteDoctor', function () {
        const note = $(this).val();
        $('#modelNote').val(note);
    });

    // Enable submit button when required fields are filled
    function updateSubmitButton() {
        const isServiceMode = $('#serviceForm').is(':visible');
        const isDoctorMode = $('#doctorForm').is(':visible');
        let isValid = false;

        if (isServiceMode) {
            isValid = $('#appointmentDate').val() && $('#serviceDropdown').val();
        } else if (isDoctorMode) {
            isValid = $('#appointmentDate').val() && $('#SelectedDoctorId').val() && $('#SelectedSlotId').val() && $('#serviceDropdownDoctor').val();
        }

        $('.submit-btn').prop('disabled', !isValid);
    }

    // Update submit button on input changes
    $('#appointmentDate, #serviceDropdown, #serviceDropdownDoctor, .time-slot.available, #noteService, #noteDoctor').on('change input click', updateSubmitButton);

    $('.submit-btn').on('click', function () {
        $(this).prop('disabled', true);
        $('#appointmentForm').submit();
    });
});