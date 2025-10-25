#!/bin/bash
# Unity Test Runner Script for Avatar Optimizer
# Based on .github/workflows/gameci.yml workflow configuration
#
# This script automates the Unity test execution process for the Avatar Optimizer package.
# It mimics the GameCI workflow for local or agent-driven test execution.

set -e  # Exit on error

# Configuration (can be overridden by environment variables)
UNITY_VERSION="${UNITY_VERSION:-2022}"
TEST_PROJECT_REF="${TEST_PROJECT_REF:-d2e10f445881af7cc806abd2fc99a0651942dbb8}"
TEST_MODE="${TEST_MODE:-EditMode}"
TIMEOUT_MINUTES="${TIMEOUT_MINUTES:-60}"
PACKAGE_PATH="${PACKAGE_PATH:-$(pwd)}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if we're in the correct repository
check_repository() {
    if [ ! -f "package.json" ]; then
        log_error "package.json not found. Are you in the Avatar Optimizer repository root?"
        exit 1
    fi
    
    local package_name=$(jq -r '.name' package.json)
    if [ "$package_name" != "com.anatawa12.avatar-optimizer" ]; then
        log_error "This doesn't appear to be the Avatar Optimizer repository"
        exit 1
    fi
    
    log_info "Repository validated: $package_name"
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check for vrc-get
    if ! command -v vrc-get &> /dev/null; then
        log_error "vrc-get is not installed or not in PATH"
        log_error "Install from: https://github.com/vrc-get/vrc-get/releases"
        exit 1
    fi
    log_info "vrc-get found: $(vrc-get --version)"
    
    # Check for jq
    if ! command -v jq &> /dev/null; then
        log_error "jq is not installed. Install with: sudo apt-get install jq"
        exit 1
    fi
    
    log_info "All prerequisites satisfied"
}

# Setup VPM repositories and dependencies
setup_dependencies() {
    log_info "Setting up VPM dependencies..."
    
    # Install VRChat Avatars SDK
    log_info "Installing VRChat Avatars SDK..."
    vrc-get install -y com.vrchat.avatars || {
        log_error "Failed to install VRChat Avatars SDK"
        exit 1
    }
    
    # Add VPM repositories
    log_info "Adding VPM repositories..."
    vrc-get repo add https://vpm.anatawa12.com/vpm.json || log_warn "anatawa12 repo may already be added"
    vrc-get repo add https://vpm.nadena.dev/vpm-prerelease.json || log_warn "nadena repo may already be added"
    
    # List installed packages
    log_info "Installed VPM packages:"
    vrc-get list || true
    
    log_info "Dependencies setup complete"
}

# Run Unity tests (requires Unity to be available)
run_unity_tests() {
    log_info "Preparing to run Unity tests..."
    log_warn "This requires Unity ${UNITY_VERSION} to be installed and licensed"
    log_warn "Tests will take 30-45 minutes to complete"
    
    # Check if unity-editor is available
    if ! command -v unity-editor &> /dev/null; then
        log_error "unity-editor command not found"
        log_error "This script requires Unity to be installed and available in PATH"
        log_error "For CI/CD, use the GameCI workflow (.github/workflows/gameci.yml)"
        exit 1
    fi
    
    log_info "Running Unity ${TEST_MODE} tests..."
    
    # Create test results directory
    mkdir -p test-results
    
    # Run tests via Unity CLI
    unity-editor \
        -runTests \
        -testPlatform "${TEST_MODE}" \
        -batchmode \
        -nographics \
        -logFile test-results/unity-test.log \
        -testResults test-results/test-results.xml \
        -projectPath . \
        -timeout $(( TIMEOUT_MINUTES * 60 )) \
        || {
            log_error "Unity tests failed or timed out"
            log_error "Check test-results/unity-test.log for details"
            exit 1
        }
    
    log_info "Unity tests completed"
}

# Analyze test results
analyze_results() {
    log_info "Analyzing test results..."
    
    if [ ! -f "test-results/test-results.xml" ]; then
        log_error "Test results file not found"
        exit 1
    fi
    
    # Parse test results (basic XML parsing)
    local total_tests=$(grep -o 'tests="[0-9]*"' test-results/test-results.xml | grep -o '[0-9]*' | head -1)
    local failed_tests=$(grep -o 'failures="[0-9]*"' test-results/test-results.xml | grep -o '[0-9]*' | head -1)
    local passed_tests=$((total_tests - failed_tests))
    
    log_info "Test Results Summary:"
    log_info "  Total Tests:  ${total_tests}"
    log_info "  Passed:       ${passed_tests}"
    log_info "  Failed:       ${failed_tests}"
    
    if [ "$failed_tests" -gt 0 ]; then
        log_error "Some tests failed. Review test-results/test-results.xml for details"
        exit 1
    else
        log_info "All tests passed!"
    fi
}

# Show usage information
usage() {
    cat << EOF
Unity Test Runner for Avatar Optimizer

Usage: $0 [options]

Options:
    -h, --help              Show this help message
    -v, --unity-version     Unity version to use (default: 2022)
    -t, --test-mode         Test mode: EditMode or PlayMode (default: EditMode)
    -m, --timeout           Timeout in minutes (default: 60)
    --skip-dependencies     Skip VPM dependency setup
    --analyze-only          Only analyze existing test results

Environment Variables:
    UNITY_VERSION          Unity version (default: 2022)
    TEST_MODE              Test mode (default: EditMode)
    TIMEOUT_MINUTES        Test timeout (default: 60)
    PACKAGE_PATH           Path to package (default: current directory)

Examples:
    $0                                    # Run with default settings
    $0 --timeout 90                       # Run with 90 minute timeout
    $0 --skip-dependencies                # Skip VPM setup
    $0 --analyze-only                     # Only analyze existing results

Based on: .github/workflows/gameci.yml

Note: This script requires Unity to be installed locally.
For CI/CD, use the GameCI GitHub Actions workflow instead.
EOF
}

# Main execution
main() {
    local skip_dependencies=false
    local analyze_only=false
    
    # Parse command line arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            -h|--help)
                usage
                exit 0
                ;;
            -v|--unity-version)
                UNITY_VERSION="$2"
                shift 2
                ;;
            -t|--test-mode)
                TEST_MODE="$2"
                shift 2
                ;;
            -m|--timeout)
                TIMEOUT_MINUTES="$2"
                shift 2
                ;;
            --skip-dependencies)
                skip_dependencies=true
                shift
                ;;
            --analyze-only)
                analyze_only=true
                shift
                ;;
            *)
                log_error "Unknown option: $1"
                usage
                exit 1
                ;;
        esac
    done
    
    log_info "Unity Test Runner for Avatar Optimizer"
    log_info "========================================"
    
    if [ "$analyze_only" = true ]; then
        analyze_results
        exit 0
    fi
    
    check_repository
    check_prerequisites
    
    if [ "$skip_dependencies" = false ]; then
        setup_dependencies
    fi
    
    run_unity_tests
    analyze_results
    
    log_info "Test execution complete!"
}

# Run main function
main "$@"
