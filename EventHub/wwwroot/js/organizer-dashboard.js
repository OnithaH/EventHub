document.addEventListener('DOMContentLoaded', function () {
    initializeDashboard();
});

/**
 * Initialize dashboard functionality
 */
function initializeDashboard() {
    initializeRevenueChart();
    animateStats();
}

/**
 * Initialize Revenue Chart using Chart.js
 */
function initializeRevenueChart() {
    const canvas = document.getElementById('revenueChart');

    if (!canvas || typeof Chart === 'undefined') {
        console.warn('Chart.js not loaded or canvas not found');
        return;
    }

    // Check if monthlyRevenueData is available
    if (typeof monthlyRevenueData === 'undefined' || !monthlyRevenueData || monthlyRevenueData.length === 0) {
        console.warn('No revenue data available');
        return;
    }

    // Prepare chart data
    const labels = monthlyRevenueData.map(item => {
        const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        return monthNames[item.month - 1] + ' ' + item.year;
    });

    const revenueData = monthlyRevenueData.map(item => item.revenue);
    const ticketsData = monthlyRevenueData.map(item => item.tickets);

    // Create chart
    const ctx = canvas.getContext('2d');
    new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Revenue (Rs.)',
                    data: revenueData,
                    borderColor: '#6366f1',
                    backgroundColor: 'rgba(99, 102, 241, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4,
                    pointRadius: 5,
                    pointHoverRadius: 7,
                    pointBackgroundColor: '#6366f1',
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2
                },
                {
                    label: 'Tickets Sold',
                    data: ticketsData,
                    borderColor: '#10b981',
                    backgroundColor: 'rgba(16, 185, 129, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4,
                    pointRadius: 5,
                    pointHoverRadius: 7,
                    pointBackgroundColor: '#10b981',
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2,
                    yAxisID: 'y1'
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                legend: {
                    display: true,
                    position: 'bottom',
                    labels: {
                        usePointStyle: true,
                        padding: 15,
                        font: {
                            size: 12,
                            weight: '500'
                        }
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    padding: 12,
                    titleFont: {
                        size: 13,
                        weight: '600'
                    },
                    bodyFont: {
                        size: 12
                    },
                    callbacks: {
                        label: function (context) {
                            let label = context.dataset.label || '';
                            if (label) {
                                label += ': ';
                            }
                            if (context.datasetIndex === 0) {
                                label += 'Rs. ' + context.parsed.y.toLocaleString();
                            } else {
                                label += context.parsed.y.toLocaleString() + ' tickets';
                            }
                            return label;
                        }
                    }
                }
            },
            scales: {
                y: {
                    type: 'linear',
                    display: true,
                    position: 'left',
                    beginAtZero: true,
                    grid: {
                        color: 'rgba(0, 0, 0, 0.05)'
                    },
                    ticks: {
                        callback: function (value) {
                            return 'Rs. ' + (value / 1000).toFixed(0) + 'K';
                        },
                        font: {
                            size: 11
                        }
                    }
                },
                y1: {
                    type: 'linear',
                    display: true,
                    position: 'right',
                    beginAtZero: true,
                    grid: {
                        drawOnChartArea: false
                    },
                    ticks: {
                        font: {
                            size: 11
                        }
                    }
                },
                x: {
                    grid: {
                        display: false
                    },
                    ticks: {
                        font: {
                            size: 11
                        }
                    }
                }
            }
        }
    });
}

/**
 * Animate statistics numbers on page load
 */
function animateStats() {
    const statElements = document.querySelectorAll('.stat-content h4');

    statElements.forEach(element => {
        const text = element.textContent;
        const numberMatch = text.match(/[\d,]+/);

        if (numberMatch) {
            const finalNumber = parseInt(numberMatch[0].replace(/,/g, ''));
            if (!isNaN(finalNumber)) {
                animateNumber(element, 0, finalNumber, 1500, text);
            }
        }
    });
}

/**
 * Animate a number from start to end
 */
function animateNumber(element, start, end, duration, originalText) {
    const range = end - start;
    const increment = range / (duration / 16);
    let current = start;

    const timer = setInterval(() => {
        current += increment;
        if (current >= end) {
            current = end;
            clearInterval(timer);
        }

        // Format the number and preserve original text structure
        const formattedNumber = Math.floor(current).toLocaleString();
        element.textContent = originalText.replace(/[\d,]+/, formattedNumber);
    }, 16);
}

/**
 * Export dashboard data (placeholder function)
 */
function exportData() {
    console.log('Export data functionality');
    // This would trigger data export in a real implementation
    alert('Export functionality will be implemented');
}

/**
 * Refresh dashboard data (placeholder function)
 */
function refreshDashboard() {
    console.log('Refreshing dashboard...');
    location.reload();
}

// Make functions available globally
window.exportData = exportData;
window.refreshDashboard = refreshDashboard;