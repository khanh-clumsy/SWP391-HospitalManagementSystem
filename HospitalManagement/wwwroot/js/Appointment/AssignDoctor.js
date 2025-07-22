function prevDoctor() {
    const container = document.querySelector('.doctor-scroll-container');
    if (!container || !canScroll(container)) return;

    container.scrollBy({ left: -200, behavior: 'smooth' });
    setTimeout(updateDoctorScrollVisibility, 350);
}

function nextDoctor() {
    const container = document.querySelector('.doctor-scroll-container');
    if (!container || !canScroll(container)) return;

    container.scrollBy({ left: 200, behavior: 'smooth' });
    setTimeout(updateDoctorScrollVisibility, 350);
}

function canScroll(container) {
    return container.scrollWidth > container.clientWidth + 1;
}

function updateDoctorScrollVisibility() {
    const container = document.querySelector('.doctor-scroll-container');
    const prevButton = document.querySelector('.carousel-nav.prev');
    const nextButton = document.querySelector('.carousel-nav.next');

    if (!container || !prevButton || !nextButton) return;

    const scrollLeft = container.scrollLeft;
    const maxScrollLeft = container.scrollWidth - container.clientWidth;
    console.log("scrollWidth:", container.scrollWidth);
    console.log("clientWidth:", container.clientWidth);
    console.log("canScroll:", canScroll(container));
    // ❌ Nếu không thể scroll (danh sách không đủ rộng) → ẩn hết nút
    if (!canScroll(container)) {
        prevButton.style.display = 'none';
        nextButton.style.display = 'none';
        return;
    }

    // ✅ Có thể scroll → xác định trạng thái từng nút
    prevButton.style.display = scrollLeft <= 1 ? 'none' : 'block';
    nextButton.style.display = scrollLeft >= maxScrollLeft - 1 ? 'none' : 'block';
}


$(document).on("click", ".open-assign-modal", function (e) {
    e.preventDefault();
    const appId = $(this).data("appointment-id");
    const date = $(this).data("appointment-date");
    $("#assignDoctorModalBody").html('<div class="text-center py-5"><div class="spinner-border text-primary"></div></div>');

    $.get("/Appointment/LoadAssignDoctorModal", { appointmentId: appId, date: date }, function (html) {
        $("#assignDoctorModalBody").html(html);
        const modal = new bootstrap.Modal(document.getElementById('assignDoctorModal'));
        modal.show();
    });
});

$(document).on('click', '.doctor-card', function () {
    $('.doctor-card').removeClass('selected');
    $(this).addClass('selected');
    const selectedId = $(this).data('doctor-id');
    $('#SelectedDoctorId').val(selectedId);
    console.log("Doctor ID: " + selectedId);
});

let timer;
$(document).on("keyup", "#doctorSearch", function () {
    clearTimeout(timer);
    const keyword = $(this).val().toLowerCase();
    timer = setTimeout(() => {
        $(".doctor-card").each(function () {
            const name = $(this).find(".card-title").text().toLowerCase();
            const dept = $(this).find(".text-muted").text().toLowerCase();
            $(this).toggle(name.includes(keyword) || dept.includes(keyword));
        });
    }, 200); // đợi 200ms sau khi gõ mới lọc
});

