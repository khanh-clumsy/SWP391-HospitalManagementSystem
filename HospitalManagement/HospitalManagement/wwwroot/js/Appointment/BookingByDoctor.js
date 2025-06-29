function getFirstMondayOfYear(year) {
    const jan1 = new Date(year, 0, 1);
    const day = jan1.getDay();
    const diff = (day === 0) ? 1 : (8 - day);
    const firstMonday = new Date(year, 0, 1 + diff - 1);
    return firstMonday.toISOString().split('T')[0];
}
function getStartOfCurrentWeek() {
    const today = new Date();
    const day = today.getDay(); // 0 = CN, 1 = T2, ..., 6 = T7
    const diff = day === 0 ? -6 : 1 - day; // Quay về thứ 2
    const monday = new Date(today);
    monday.setDate(today.getDate() + diff);
    return monday.toISOString().split('T')[0]; // yyyy-MM-dd
}

function loadDoctorSchedule(doctorId) {
    const year = new Date().getFullYear();
    const weekStart = getStartOfCurrentWeek();
    $.ajax({
        url: '/Appointment/GetDoctorScheduleTable',
        type: 'GET',
        data: {
            doctorId: doctorId,
            year: year,
            weekStart: weekStart,
            includePending: false
        },
        success: function (html) {
            $('#scheduleTable').html(html);
            $('#scheduleSection').show();
        },
        error: function () {
            alert("Lỗi khi tải lịch của bác sĩ.");
        }
    });
}

function changeWeek(offset) {
    const weekDropdown = document.getElementById("weekDropdown");
    const options = weekDropdown.options;
    const current = weekDropdown.selectedIndex;
    const newIndex = current + offset;

    if (newIndex >= 0 && newIndex < options.length) {
        weekDropdown.selectedIndex = newIndex;
        updateSchedule();
    }
}

function updateSchedule(newyear = null) {
    // Lấy giá trị từ dropdown hoặc giữ nguyên nếu không thay đổi
    let year = newyear || document.getElementById("yearDropdown").value;
    let weekStart = newyear ? getFirstMondayOfYear(newyear) : document.getElementById("weekDropdown").value;
    const doctorId = $('#SelectedDoctorId').val(); // Đảm bảo lấy đúng doctorId

    if (!doctorId) {
        console.error("Không có bác sĩ được chọn");
        return;
    }

    $.ajax({
        url: '/Appointment/GetDoctorScheduleTable',
        type: 'GET',
        data: {
            doctorId: doctorId,
            year: year,
            weekStart: weekStart,
            includePending: false
        },
        beforeSend: function () {
            $('#scheduleTable').html('<div class="text-center"><i class="fas fa-spinner fa-spin"></i> Đang tải...</div>');
        },
        success: function (html) {
            setTimeout(function () {
                $('#scheduleTable').html(html);
                $('#scheduleSection').show();

                // Giữ highlight bác sĩ
                $('.doctor-card').removeClass('selected');
                $(`.doctor-card[data-doctor-id="${doctorId}"]`).addClass('selected');
            }, 200);
        },
        error: function (xhr, status, error) {
            console.error("Lỗi AJAX:", status, error, xhr.responseText);
            alert("Lỗi khi tải lịch mới.");
        }
    });
}
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

$(document).ready(function () {
    // Ẩn các khu vực ban đầu
    $('#time-slots-container').hide();
    $('.service-title, .service-dropdown').hide();

    // Cập nhật trạng thái nút Đặt Hẹn
    function updateSubmitButton() {
        const doctorId = $('#SelectedDoctorId').val();
        const date = $('#AppointmentDate').val();
        const slotId = $('#SelectedSlotId').val();

        const isService = $('#serviceRadio').is(':checked');
        const isPackage = $('#packageRadio').is(':checked');
        const serviceId = $('#serviceDropdown').val();
        const packageId = $('#packageDropdown').val();

        let valid = doctorId && date && slotId;

        if (isService) {
            valid = valid && serviceId;
        } else if (isPackage) {
            valid = valid && packageId;
        }

        console.log("updateSubmitButton debug =>", {
            doctorId,
            date,
            slotId,
            isService,
            serviceId,
            isPackage,
            packageId,
            finalValid: valid
        });

        $('.submit-btn').prop('disabled', !valid);
    }
    const selectedDepartment = $('#departmentDropdown').val()?.trim();
    if (selectedDepartment) {
        $('#departmentDropdown').trigger('change');
    }

    // Khi chọn chuyên khoa -> load danh sách bác sĩ
    $('#departmentDropdown').on('change', function () {
        var departmentName = $(this).val(); // tên khoa

        if (!departmentName) {
            $('#SelectedDoctorId, #AppointmentDate, #SelectedSlotId').val('');
            $('#serviceDropdown, #packageDropdown').val('');
            $('input[name="ServiceType"]').prop('checked', false);

            // Ẩn các vùng liên quan
            $('#package-tests-container').hide().empty();
            $('.test-title, .service-choice').hide();
            $('.schedule-slot').removeClass('selected');
            $('#doctorList').empty();
            $('#scheduleTable').empty();
            $('#scheduleSection').hide();
            updateSubmitButton();
            return;
        }

        $.ajax({
            url: '/Appointment/GetDoctorsByDepartment', // hoặc '/Appointment/GetDoctorsByDate'
            type: 'GET',
            data: { department: departmentName }, // hoặc { date: selectedDate }
            success: function (data) {
                const $list = $('#doctorList');
                $list.empty();
                if (!data || data.length === 0) {
                    $list.append('<p class="text-muted text-center my-3">Không có bác sĩ trống trong ngày/khoa này</p>');
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
                        <h6 class="card-title mb-0">${doctor.doctorName}</h6>
                        <p class="text-muted small mb-0">${doctor.departmentName}</p>
                    </div>
                </div>`;
                    $list.append(card);
                });
                $('.doctor-section').show();
                updateDoctorScrollVisibility();
                const selectedDoctorId = $('#SelectedDoctorId').val();
                if (selectedDoctorId) {
                    const card = $(`.doctor-card[data-doctor-id="${selectedDoctorId}"]`);
                    if (card.length > 0) {
                        card.trigger('click');
                    }
                }
            },
            error: function (xhr, status, error) {
                console.error("Lỗi khi tải bác sĩ:", status, error);
            }
        });
    });

    // Bắt sự kiện click chọn bác sĩ
    $(document).on('click', '.doctor-card', function () {
        $('.doctor-card').removeClass('selected');
        $(this).addClass('selected');
        const doctorId = $(this).data('doctor-id');
        $('#SelectedDoctorId').val(doctorId);
        console.log("Doctor ID: " + doctorId);
        loadDoctorSchedule(doctorId);
        updateSubmitButton();
    });

    // Lọc tên bác sĩ
    $('#doctorSearch').on('input', function () {
        const keyword = $(this).val();
        const departmentName = $('#departmentDropdown').val();

        $.ajax({
            url: '/Appointment/SearchDoctors',
            type: 'GET',
            data: {
                keyword: keyword,
                departmentName: departmentName
            },
            success: function (data) {
                const $list = $('#doctorList');
                $list.empty();

                if (!data || data.length === 0) {
                    $list.append('<p class="text-muted text-center my-3">Không tìm thấy bác sĩ phù hợp.</p>');
                    updateDoctorScrollVisibility();
                    return;
                }

                data.forEach(function (doctor) {
                    const card = `
					<div class="card text-center shadow-sm doctor-card" data-doctor-id="${doctor.doctorId}" style="min-width: 180px; cursor: pointer;">
						<div class="card-body">
							<img src="${doctor.profileImage ? 'data:image/png;base64,' + doctor.profileImage : '~/img/logo.jpg'}"
								class="img-fluid rounded-circle mb-2" style="width: 60px; height: 60px;" />
							<h6 class="card-title mb-0">${doctor.fullName}</h6>
							<p class="text-muted small mb-0">${doctor.departmentName}</p>
						</div>
					</div>`;
                    $list.append(card);
                });

                $('.doctor-section').show();
                updateDoctorScrollVisibility();
            },
            error: function () {
                console.error("Không thể tìm bác sĩ.");
            }
        });
    });
    // Toggle service / package dropdowns
    function toggleDropdowns(reset = true) {
        const isService = $('#serviceRadio').is(':checked');
        $('.service-dropdown').toggle(isService);
        $('.package-dropdown').toggle(!isService);

        if (reset) {
            // Chỉ clear khi thực sự toggle
            $('#serviceDropdown').val('');
            $('#packageDropdown').val('');
            $('.test-title').hide();
            $('#package-tests-container').hide().empty();
        }
    }

    $(document).on('click', '.schedule-slot', function (e) {
        e.preventDefault();
        const day = $(this).data('day');
        const slotId = $(this).data('slot-id');
        console.log("Slot được chọn:", { day, slotId });

        $('.schedule-slot').removeClass('selected');
        $(this).addClass('selected');

        // Gán trước
        $('#SelectedSlotId').val(slotId);
        $('#AppointmentDate').val(day);
        console.log("Đã gán #SelectedSlotId =", $('#SelectedSlotId').val());

        // Hiện giao diện
        $('.section-title').show();
        $('.service-choice').show();
        $('#package-tests-container').hide();
        $('.test-title').hide();
        updateSubmitButton();
    });


    $('input[name="ServiceType"]').on('change', function () {
        // Toggle chỉ clear dropdowns/tests thôi
        toggleDropdowns(true);
        // Luôn load lại slot ngay khi đổi radio
        updateSubmitButton();
    });

    $(document).on('change', '#serviceDropdown, #packageDropdown', function () {
        const isPackage = this.id === 'packageDropdown';
        const id = $(this).val();
        console.log(`Selected ${isPackage ? 'package' : 'service'} ID:`, id);
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
                    updateSubmitButton();
                })
                .fail(function () {
                    $('#package-tests-container').html('<div class="text-danger">Không thể tải danh sách xét nghiệm.</div>');
                });
        }
        else {
            $('.test-title').hide();
            $('#package-tests-container').hide().empty();
            $('#SelectedServiceId').val(id);
            updateSubmitButton();
        }
    });
    // Submit form
    $('.submit-btn').on('click', function (e) {
        e.preventDefault(); 
        $(this).prop('disabled', true);
        $('#doctorForm').submit();
    });

    if ($('#departmentDropdown').val()?.trim()) {
        $('#departmentDropdown').trigger('change');
    }
    if ($('#SelectedDoctorId').val()?.trim()) {
        const doctorId = $('#SelectedDoctorId').val();
        $('#SelectedDoctorId').val(doctorId);
        $(`.doctor-card[data-doctor-id="${doctorId}"]`).trigger('click');
    }
    // Khởi tạo
    updateSubmitButton();

    
});
    